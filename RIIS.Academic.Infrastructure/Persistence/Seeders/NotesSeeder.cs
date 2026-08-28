using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class NotesSeeder
{
    private const string ResourceName = "notes-cc-ccon-2025-2026.json";
    private const string SeederUser = "Seeder notes CC/CCON 2025-2026";

    public static async Task SeedCcCcon2025_2026Async(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await StudentsSeeder.SeedStudents2025_2026Async(context, cancellationToken);

        var data = await LoadSeedDataAsync(cancellationToken);

        var anneeAcademique = await context.AnneesAcademiques
            .FirstOrDefaultAsync(x => x.Libelle == data.AnneeAcademique, cancellationToken)
            ?? throw new InvalidOperationException($"L'année académique '{data.AnneeAcademique}' est introuvable.");

        var inscriptionsParMatricule = await context.Inscriptions
            .Include(x => x.Etudiant)
            .Include(x => x.ParcoursAcademique)
            .Where(x => x.AnneeAcademiqueId == anneeAcademique.Id && x.Etudiant.Matricule != null)
            .ToDictionaryAsync(x => Normalize(x.Etudiant.Matricule!), StringComparer.OrdinalIgnoreCase, cancellationToken);

        var programmeCache = new Dictionary<ProgrammeCacheKey, ProgrammeSeedContext>();

        foreach (var section in data.Sections)
        {
            var subjectsByCode = section.Subjects.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

            foreach (var student in section.Students)
            {
                var matricule = Normalize(student.Matricule);

                if (!inscriptionsParMatricule.TryGetValue(matricule, out var inscription))
                {
                    throw new InvalidOperationException(
                        $"Aucune inscription trouvée pour le matricule '{student.Matricule}' en {data.AnneeAcademique}.");
                }

                var programmeKey = new ProgrammeCacheKey(inscription.ParcoursAcademiqueId, data.SemestreNumero);

                if (!programmeCache.TryGetValue(programmeKey, out var programme))
                {
                    programme = await ResolveProgrammeAsync(
                        context,
                        inscription.ParcoursAcademique,
                        data,
                        cancellationToken);

                    programmeCache.Add(programmeKey, programme);
                }

                if (inscription.MaquettePedagogiqueId is null)
                {
                    inscription.MaquettePedagogiqueId = programme.Maquette.Id;
                }

                foreach (var note in student.Notes)
                {
                    if (!subjectsByCode.TryGetValue(note.ElementConstitutifCode, out var subject))
                    {
                        throw new InvalidOperationException(
                            $"L'EC '{note.ElementConstitutifCode}' est introuvable dans la section '{section.FiliereLibelle}'.");
                    }

                    var elementConstitutif = await ResolveElementConstitutifAsync(
                        context,
                        programme.Semestre,
                        subject,
                        cancellationToken);

                    var evaluationCcon = await EnsureEvaluationAsync(
                        context,
                        anneeAcademique.Id,
                        elementConstitutif,
                        TypeEvaluation.ControleContinu,
                        "CCON1",
                        "CCON 1",
                        20m,
                        data.SourceDocument,
                        cancellationToken);

                    var evaluationCc = await EnsureEvaluationAsync(
                        context,
                        anneeAcademique.Id,
                        elementConstitutif,
                        TypeEvaluation.ControleConnaissance,
                        "CC1",
                        "CC 1",
                        10m,
                        data.SourceDocument,
                        cancellationToken);

                    await UpsertNoteAsync(
                        context,
                        evaluationCcon.Id,
                        inscription.Id,
                        note.Ccon,
                        data.SourceDocument,
                        cancellationToken);

                    await UpsertNoteAsync(
                        context,
                        evaluationCc.Id,
                        inscription.Id,
                        note.Cc,
                        data.SourceDocument,
                        cancellationToken);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<NotesSeedData> LoadSeedDataAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(NotesSeeder).Assembly;
        var resourceFullName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(ResourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"La ressource embarquée '{ResourceName}' est introuvable.");

        await using var stream = assembly.GetManifestResourceStream(resourceFullName)
            ?? throw new FileNotFoundException($"La ressource embarquée '{ResourceName}' est introuvable.");

        var data = await JsonSerializer.DeserializeAsync<NotesSeedData>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        return data ?? throw new InvalidOperationException($"La ressource '{ResourceName}' est vide ou invalide.");
    }

    private static async Task<ProgrammeSeedContext> ResolveProgrammeAsync(
        RiisAcademicDbContext context,
        ParcoursAcademique parcoursAcademique,
        NotesSeedData data,
        CancellationToken cancellationToken)
    {
        var maquette = await ResolveMaquetteAsync(
            context,
            parcoursAcademique,
            data,
            cancellationToken);

        var semestre = await ResolveSemestreAsync(
            context,
            maquette,
            data.SemestreNumero,
            cancellationToken);

        return new ProgrammeSeedContext(maquette, semestre);
    }

    private static async Task<MaquettePedagogique> ResolveMaquetteAsync(
        RiisAcademicDbContext context,
        ParcoursAcademique parcoursAcademique,
        NotesSeedData data,
        CancellationToken cancellationToken)
    {
        var maquette = await context.MaquettesPedagogiques
            .FirstOrDefaultAsync(
                x => x.ParcoursAcademiqueId == parcoursAcademique.Id
                    && x.Version == data.AnneeAcademique,
                cancellationToken);

        if (maquette is null)
        {
            throw new InvalidOperationException(
                $"Aucune maquette existante trouvée pour le parcours '{parcoursAcademique.Code}' et la version '{data.AnneeAcademique}'. Créez d'abord la maquette, ses semestres, ses UE et ses EC avant d'importer les notes.");
        }

        return maquette;
    }

    private static async Task<SemestrePedagogique> ResolveSemestreAsync(
        RiisAcademicDbContext context,
        MaquettePedagogique maquette,
        byte semestreNumero,
        CancellationToken cancellationToken)
    {
        var semestre = await context.SemestresPedagogiques
            .FirstOrDefaultAsync(
                x => x.MaquettePedagogiqueId == maquette.Id && x.Numero == semestreNumero,
                cancellationToken);

        if (semestre is null)
        {
            throw new InvalidOperationException(
                $"Le semestre {semestreNumero} est introuvable dans la maquette '{maquette.Code}'. Créez d'abord le programme pédagogique avant d'importer les notes.");
        }

        return semestre;
    }

    private static async Task<ElementConstitutif> ResolveElementConstitutifAsync(
        RiisAcademicDbContext context,
        SemestrePedagogique semestre,
        SubjectSeedData subject,
        CancellationToken cancellationToken)
    {
        var elementsConstitutifs = await context.ElementsConstitutifs
            .Include(x => x.UniteEnseignement)
            .Where(x => x.UniteEnseignement.SemestrePedagogiqueId == semestre.Id)
            .ToListAsync(cancellationToken);

        var elementConstitutif = elementsConstitutifs
            .FirstOrDefault(x => string.Equals(x.Code, subject.Code, StringComparison.OrdinalIgnoreCase))
            ?? elementsConstitutifs.FirstOrDefault(x => SameLibelle(x.Libelle, subject.Libelle));

        return elementConstitutif
            ?? throw new InvalidOperationException(
                $"L'EC '{subject.Libelle}' ({subject.Code}) est introuvable dans le semestre {semestre.Numero}. Corrigez la maquette avant d'importer les notes.");
    }

    private static async Task<EvaluationAcademique> EnsureEvaluationAsync(
        RiisAcademicDbContext context,
        long anneeAcademiqueId,
        ElementConstitutif elementConstitutif,
        TypeEvaluation type,
        string codePrefix,
        string libellePrefix,
        decimal ponderationPourcentage,
        string sourceDocument,
        CancellationToken cancellationToken)
    {
        var evaluation = await context.EvaluationsAcademiques
            .FirstOrDefaultAsync(
                x => x.AnneeAcademiqueId == anneeAcademiqueId
                    && x.ElementConstitutifId == elementConstitutif.Id
                    && x.Type == type
                    && x.Numero == 1,
                cancellationToken);

        var code = $"{codePrefix}-{elementConstitutif.Code}";
        var libelle = $"{libellePrefix} - {elementConstitutif.Libelle}";

        if (evaluation is not null)
        {
            evaluation.Code = code;
            evaluation.Libelle = libelle;
            evaluation.Bareme = 20m;
            evaluation.PonderationPourcentage = ponderationPourcentage;
            evaluation.Observation = $"Import {sourceDocument}";

            await context.SaveChangesAsync(cancellationToken);
            return evaluation;
        }

        evaluation = new EvaluationAcademique
        {
            AnneeAcademiqueId = anneeAcademiqueId,
            ElementConstitutifId = elementConstitutif.Id,
            Type = type,
            Numero = 1,
            Code = code,
            Libelle = libelle,
            Bareme = 20m,
            PonderationPourcentage = ponderationPourcentage,
            Observation = $"Import {sourceDocument}"
        };

        context.EvaluationsAcademiques.Add(evaluation);
        await context.SaveChangesAsync(cancellationToken);

        return evaluation;
    }

    private static async Task UpsertNoteAsync(
        RiisAcademicDbContext context,
        long evaluationAcademiqueId,
        long inscriptionId,
        string? valeurSource,
        string sourceDocument,
        CancellationToken cancellationToken)
    {
        var parsedNote = ParseNote(valeurSource);

        if (parsedNote is null)
        {
            return;
        }

        var note = await context.NotesEvaluations
            .FirstOrDefaultAsync(
                x => x.EvaluationAcademiqueId == evaluationAcademiqueId && x.InscriptionId == inscriptionId,
                cancellationToken);

        if (note is null)
        {
            note = new NoteEvaluation
            {
                EvaluationAcademiqueId = evaluationAcademiqueId,
                InscriptionId = inscriptionId
            };

            context.NotesEvaluations.Add(note);
        }

        var parsed = parsedNote.GetValueOrDefault();

        note.Valeur = parsed.Value;
        note.StatutPresence = parsed.StatutPresence;
        note.Observation = $"Import {sourceDocument}; valeur source: {NormalizeNullable(valeurSource) ?? "vide"}.";
        note.SaisieLeUtc = DateTime.UtcNow;
        note.SaisiePar = SeederUser;
    }

    private static ParsedNote? ParseNote(string? valeurSource)
    {
        var normalized = NormalizeNullable(valeurSource);

        if (normalized is null)
        {
            return null;
        }

        if (normalized == "//")
        {
            return new ParsedNote(null, StatutPresenceEvaluation.AbsenceNonJustifiee);
        }

        var decimalText = normalized.Replace(',', '.');

        if (!decimal.TryParse(decimalText, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidOperationException($"La note '{valeurSource}' est invalide.");
        }

        if (value is < 0m or > 20m)
        {
            throw new InvalidOperationException($"La note '{valeurSource}' est hors barème 0-20.");
        }

        return new ParsedNote(value, StatutPresenceEvaluation.Present);
    }

    private static string BuildMaquetteCode(string parcoursAcademiqueCode)
    {
        var normalized = Normalize(parcoursAcademiqueCode).ToUpperInvariant();
        var code = $"MAQ-N-{normalized}";

        return code.Length <= 40
            ? code
            : code[..40];
    }

    private static string Normalize(string value)
        => string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static bool SameLibelle(string left, string right)
        => string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : Normalize(value);

    private readonly record struct ProgrammeCacheKey(long ParcoursAcademiqueId, byte SemestreNumero);

    private readonly record struct ParsedNote(decimal? Value, StatutPresenceEvaluation StatutPresence);

    private readonly record struct ProgrammeSeedContext(
        MaquettePedagogique Maquette,
        SemestrePedagogique Semestre);

    private class NotesSeedData
    {
        public string SourceDocument { get; set; } = string.Empty;
        public string AnneeAcademique { get; set; } = string.Empty;
        public byte NiveauNumero { get; set; }
        public byte SemestreNumero { get; set; }
        public List<NotesSectionSeedData> Sections { get; set; } = [];
    }

    private class NotesSectionSeedData
    {
        public string FiliereLibelle { get; set; } = string.Empty;
        public string ParcoursAcademiqueCode { get; set; } = string.Empty;
        public byte NiveauNumero { get; set; }
        public byte SemestreNumero { get; set; }
        public string MaquetteCode { get; set; } = string.Empty;
        public string MaquetteLibelle { get; set; } = string.Empty;
        public string UeCode { get; set; } = string.Empty;
        public string UeLibelle { get; set; } = string.Empty;
        public List<SubjectSeedData> Subjects { get; set; } = [];
        public List<StudentNotesSeedData> Students { get; set; } = [];
    }

    private class SubjectSeedData
    {
        public string Code { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public short OrdreAffichage { get; set; }
    }

    private class StudentNotesSeedData
    {
        public string Matricule { get; set; } = string.Empty;
        public string NomCompletSource { get; set; } = string.Empty;
        public List<StudentElementNotesSeedData> Notes { get; set; } = [];
    }

    private class StudentElementNotesSeedData
    {
        public string ElementConstitutifCode { get; set; } = string.Empty;
        public string? Ccon { get; set; }
        public string? Cc { get; set; }
    }
}
