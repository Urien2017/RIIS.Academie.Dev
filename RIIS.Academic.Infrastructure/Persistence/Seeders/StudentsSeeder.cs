using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class StudentsSeeder
{
    private const string ResourceName = "student-lists-2025-2026.json";
    private static readonly DateOnly DefaultDateNaissance = new(1900, 1, 1);
    private static readonly DateOnly DefaultDateInscription = new(2025, 9, 1);

    public static async Task SeedStudents2025_2026Async(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await DataSeeder.SeedAsync(context, cancellationToken);

        var data = await LoadSeedDataAsync(cancellationToken);

        var anneeAcademique = await EnsureAnneeAcademiqueAsync(context, data.AnneeAcademique, cancellationToken);
        await EnsureReferencesAsync(context, anneeAcademique, data, cancellationToken);

        foreach (var classeData in data.Classes)
        {
            var classe = await EnsureClassePedagogiqueAsync(context, anneeAcademique, classeData, cancellationToken);

            foreach (var studentData in classeData.Students)
            {
                var etudiant = await EnsureEtudiantAsync(context, studentData, cancellationToken);
                await EnsureInscriptionAsync(context, anneeAcademique, etudiant, classe, classeData, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<StudentListsSeedData> LoadSeedDataAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(StudentsSeeder).Assembly;
        var resourceFullName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(ResourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"La ressource embarquée '{ResourceName}' est introuvable.");

        await using var stream = assembly.GetManifestResourceStream(resourceFullName)
            ?? throw new FileNotFoundException($"La ressource embarquée '{ResourceName}' est introuvable.");

        var data = await JsonSerializer.DeserializeAsync<StudentListsSeedData>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        return data ?? throw new InvalidOperationException($"La ressource '{ResourceName}' est vide ou invalide.");
    }

    private static async Task<AnneeAcademique> EnsureAnneeAcademiqueAsync(
        RiisAcademicDbContext context,
        string libelle,
        CancellationToken cancellationToken)
    {
        var normalizedLibelle = Normalize(libelle);
        var parts = normalizedLibelle.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2
            || !short.TryParse(parts[0], out var anneeDebut)
            || !short.TryParse(parts[1], out var anneeFin))
        {
            throw new InvalidOperationException($"L'année académique '{libelle}' est invalide.");
        }

        var anneeAcademique = await context.AnneesAcademiques
            .FirstOrDefaultAsync(x => x.Libelle == normalizedLibelle, cancellationToken);

        if (anneeAcademique is not null)
        {
            return anneeAcademique;
        }

        anneeAcademique = new AnneeAcademique
        {
            Libelle = normalizedLibelle,
            AnneeDebut = anneeDebut,
            AnneeFin = anneeFin,
            EstActive = true
        };

        context.AnneesAcademiques.Add(anneeAcademique);

        await context.SaveChangesAsync(cancellationToken);

        return anneeAcademique;
    }

    private static async Task EnsureReferencesAsync(
        RiisAcademicDbContext context,
        AnneeAcademique anneeAcademique,
        StudentListsSeedData data,
        CancellationToken cancellationToken)
    {
        await EnsureCycleAsync(context, "LICENCE", "Licence", 3, cancellationToken);
        await EnsureFiliereAsync(context, "GESTION", "Gestion", cancellationToken);
        await EnsureSpecialiteAsync(context, "GESTION", "COFI", "Comptabilité et finance", cancellationToken);
        await EnsureSpecialiteAsync(context, "INFORMATIQUE", "IGL", "Informatique génie logiciel", cancellationToken);

        foreach (var classe in data.Classes)
        {
            await EnsureNiveauAsync(context, classe.NiveauNumero, cancellationToken);
            await EnsureParcoursAcademiqueAsync(context, anneeAcademique, classe, cancellationToken);
        }
    }

    private static async Task EnsureCycleAsync(
        RiisAcademicDbContext context,
        string code,
        string libelle,
        short ordreAffichage,
        CancellationToken cancellationToken)
    {
        var exists = await context.CyclesFormation.AnyAsync(x => x.Code == code, cancellationToken);

        if (exists)
        {
            return;
        }

        context.CyclesFormation.Add(new CycleFormation
        {
            Code = code,
            Libelle = libelle,
            OrdreAffichage = ordreAffichage,
            EstActif = true
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureFiliereAsync(
        RiisAcademicDbContext context,
        string code,
        string libelle,
        CancellationToken cancellationToken)
    {
        var exists = await context.Filieres.AnyAsync(x => x.Code == code, cancellationToken);

        if (exists)
        {
            return;
        }

        context.Filieres.Add(new Filiere
        {
            Code = code,
            Libelle = libelle,
            EstActive = true
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSpecialiteAsync(
        RiisAcademicDbContext context,
        string filiereCode,
        string code,
        string libelle,
        CancellationToken cancellationToken)
    {
        var filiere = await context.Filieres
            .FirstOrDefaultAsync(x => x.Code == filiereCode, cancellationToken)
            ?? throw new InvalidOperationException($"La filière '{filiereCode}' est introuvable.");

        var exists = await context.Specialites
            .AnyAsync(x => x.FiliereId == filiere.Id && x.Code == code, cancellationToken);

        if (exists)
        {
            return;
        }

        context.Specialites.Add(new Specialite
        {
            FiliereId = filiere.Id,
            Code = code,
            Libelle = libelle,
            EstActive = true
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureNiveauAsync(
        RiisAcademicDbContext context,
        byte numero,
        CancellationToken cancellationToken)
    {
        var exists = await context.NiveauxEtude.AnyAsync(x => x.Numero == numero, cancellationToken);

        if (exists)
        {
            return;
        }

        context.NiveauxEtude.Add(new NiveauEtude
        {
            Numero = numero,
            Libelle = $"Niveau {numero}",
            EstActif = true
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureParcoursAcademiqueAsync(
        RiisAcademicDbContext context,
        AnneeAcademique anneeAcademique,
        StudentClassSeedData classe,
        CancellationToken cancellationToken)
    {
        var cycle = await context.CyclesFormation
            .FirstOrDefaultAsync(x => x.Code == classe.CycleCode, cancellationToken)
            ?? throw new InvalidOperationException($"Le cycle '{classe.CycleCode}' est introuvable.");

        var filiere = await context.Filieres
            .FirstOrDefaultAsync(x => x.Code == classe.FiliereCode, cancellationToken)
            ?? throw new InvalidOperationException($"La filière '{classe.FiliereCode}' est introuvable.");
        var niveau = await context.NiveauxEtude
            .FirstOrDefaultAsync(x => x.Numero == classe.NiveauNumero, cancellationToken)
            ?? throw new InvalidOperationException($"Le niveau '{classe.NiveauNumero}' est introuvable.");

        var specialiteCode = RequireSpecialiteCode(classe);
        var specialite = await context.Specialites
            .FirstOrDefaultAsync(
                x => x.FiliereId == filiere.Id && x.Code == specialiteCode,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"La spécialité '{specialiteCode}' est introuvable pour la filière '{classe.FiliereCode}'.");

        var exists = await context.ParcoursAcademiques
            .AnyAsync(
                x => x.CycleFormationId == cycle.Id
                    && x.AnneeAcademiqueId == anneeAcademique.Id
                    && x.NiveauEtudeId == niveau.Id
                    && x.FiliereId == filiere.Id
                    && x.SpecialiteId == specialite.Id,
                cancellationToken);

        if (exists)
        {
            return;
        }

        var code = BuildParcoursAcademiqueCode(anneeAcademique.AnneeDebut, classe.CycleCode, classe.NiveauNumero, classe.FiliereCode, specialiteCode);

        context.ParcoursAcademiques.Add(new ParcoursAcademique
        {
            AnneeAcademiqueId = anneeAcademique.Id,
            CycleFormationId = cycle.Id,
            NiveauEtudeId = niveau.Id,
            FiliereId = filiere.Id,
            SpecialiteId = specialite.Id,
            Code = code,
            Libelle = BuildParcoursAcademiqueLibelle(anneeAcademique.Libelle, classe.CycleCode, classe.NiveauNumero, filiere.Libelle, specialite.Libelle),
            EstOuverteInscription = true,
            EstActive = true
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<ClassePedagogique> EnsureClassePedagogiqueAsync(
        RiisAcademicDbContext context,
        AnneeAcademique anneeAcademique,
        StudentClassSeedData classeData,
        CancellationToken cancellationToken)
    {
        var niveau = await context.NiveauxEtude
            .FirstOrDefaultAsync(x => x.Numero == classeData.NiveauNumero, cancellationToken)
            ?? throw new InvalidOperationException($"Le niveau '{classeData.NiveauNumero}' est introuvable.");

        var parcoursAcademique = await GetParcoursAcademiqueAsync(context, anneeAcademique, classeData, cancellationToken);

        var classe = await context.ClassesPedagogiques
            .FirstOrDefaultAsync(
                x => x.AnneeAcademiqueId == anneeAcademique.Id
                    && x.ParcoursAcademiqueId == parcoursAcademique.Id
                    && x.NiveauEtudeId == niveau.Id
                    && x.Code == classeData.Code,
                cancellationToken);

        if (classe is not null)
        {
            classe.Libelle = classeData.Libelle;
            classe.EstActive = true;
            return classe;
        }

        classe = new ClassePedagogique
        {
            AnneeAcademiqueId = anneeAcademique.Id,
            ParcoursAcademiqueId = parcoursAcademique.Id,
            NiveauEtudeId = niveau.Id,
            Code = classeData.Code,
            Libelle = classeData.Libelle,
            EstActive = true
        };

        context.ClassesPedagogiques.Add(classe);
        await context.SaveChangesAsync(cancellationToken);

        return classe;
    }

    private static async Task<ParcoursAcademique> GetParcoursAcademiqueAsync(
        RiisAcademicDbContext context,
        AnneeAcademique anneeAcademique,
        StudentClassSeedData classe,
        CancellationToken cancellationToken)
    {
        var cycle = await context.CyclesFormation
            .FirstOrDefaultAsync(x => x.Code == classe.CycleCode, cancellationToken)
            ?? throw new InvalidOperationException($"Le cycle '{classe.CycleCode}' est introuvable.");

        var filiere = await context.Filieres
            .FirstOrDefaultAsync(x => x.Code == classe.FiliereCode, cancellationToken)
            ?? throw new InvalidOperationException($"La filière '{classe.FiliereCode}' est introuvable.");
        var niveau = await context.NiveauxEtude
            .FirstOrDefaultAsync(x => x.Numero == classe.NiveauNumero, cancellationToken)
            ?? throw new InvalidOperationException($"Le niveau '{classe.NiveauNumero}' est introuvable.");

        var specialiteCode = RequireSpecialiteCode(classe);
        var specialite = await context.Specialites
            .FirstOrDefaultAsync(
                x => x.FiliereId == filiere.Id && x.Code == specialiteCode,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"La spécialité '{specialiteCode}' est introuvable pour la filière '{classe.FiliereCode}'.");

        return await context.ParcoursAcademiques
            .FirstOrDefaultAsync(
                x => x.CycleFormationId == cycle.Id
                    && x.AnneeAcademiqueId == anneeAcademique.Id
                    && x.NiveauEtudeId == niveau.Id
                    && x.FiliereId == filiere.Id
                    && x.SpecialiteId == specialite.Id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Le parcours académique '{BuildParcoursAcademiqueCode(anneeAcademique.AnneeDebut, classe.CycleCode, classe.NiveauNumero, classe.FiliereCode, specialiteCode)}' est introuvable.");
    }

    private static async Task<Etudiant> EnsureEtudiantAsync(
        RiisAcademicDbContext context,
        StudentSeedData studentData,
        CancellationToken cancellationToken)
    {
        var matricule = NormalizeNullable(studentData.Matricule);
        var (nom, prenoms) = SplitNomPrenoms(studentData.NomPrenoms);
        var dateNaissance = ParseDateOrDefault(studentData.DateNaissance, DefaultDateNaissance);
        var lieuNaissance = NormalizeNullable(studentData.LieuNaissance) ?? "Non renseigné";

        Etudiant? etudiant = null;

        if (matricule is not null)
        {
            etudiant = await context.Etudiants
                .FirstOrDefaultAsync(x => x.Matricule == matricule, cancellationToken);
        }

        etudiant ??= await context.Etudiants
            .FirstOrDefaultAsync(
                x => x.Matricule == null
                    && x.Nom == nom
                    && x.Prenoms == prenoms
                    && x.DateNaissance == dateNaissance,
                cancellationToken);

        if (etudiant is null)
        {
            etudiant = new Etudiant
            {
                Matricule = matricule,
                Nom = nom,
                Prenoms = prenoms,
                DateNaissance = dateNaissance,
                LieuNaissance = lieuNaissance,
                Sexe = ParseSexe(studentData.Sexe),
                AptitudeMedicale = AptitudeMedicale.NonRenseignee,
                Nationalite = "Camerounaise",
                TelephonePrincipal = "Non renseigné"
            };

            context.Etudiants.Add(etudiant);
            await context.SaveChangesAsync(cancellationToken);

            return etudiant;
        }

        etudiant.Matricule ??= matricule;
        etudiant.Nom = nom;
        etudiant.Prenoms = prenoms;
        etudiant.DateNaissance = dateNaissance;
        etudiant.LieuNaissance = lieuNaissance;
        etudiant.Sexe = ParseSexe(studentData.Sexe);
        etudiant.Nationalite = string.IsNullOrWhiteSpace(etudiant.Nationalite)
            ? "Camerounaise"
            : etudiant.Nationalite;
        etudiant.TelephonePrincipal = string.IsNullOrWhiteSpace(etudiant.TelephonePrincipal)
            ? "Non renseigné"
            : etudiant.TelephonePrincipal;

        return etudiant;
    }

    private static async Task EnsureInscriptionAsync(
        RiisAcademicDbContext context,
        AnneeAcademique anneeAcademique,
        Etudiant etudiant,
        ClassePedagogique classe,
        StudentClassSeedData classeData,
        CancellationToken cancellationToken)
    {
        var dateInscription = ParseDateOrDefault(classeData.DateInscription, DefaultDateInscription);

        var inscription = await context.Inscriptions
            .FirstOrDefaultAsync(
                x => x.AnneeAcademiqueId == anneeAcademique.Id
                    && x.EtudiantId == etudiant.Id,
                cancellationToken);

        if (inscription is not null)
        {
            inscription.ParcoursAcademiqueId = classe.ParcoursAcademiqueId;
            inscription.NiveauEtudeId = classe.NiveauEtudeId;
            inscription.ClassePedagogiqueId = classe.Id;
            inscription.DateInscription = dateInscription;
            inscription.Statut = StatutInscription.Validee;
            return;
        }

        context.Inscriptions.Add(new Inscription
        {
            AnneeAcademiqueId = anneeAcademique.Id,
            EtudiantId = etudiant.Id,
            ParcoursAcademiqueId = classe.ParcoursAcademiqueId,
            NiveauEtudeId = classe.NiveauEtudeId,
            ClassePedagogiqueId = classe.Id,
            DateInscription = dateInscription,
            Statut = StatutInscription.Validee,
            Observation = $"Import liste étudiants 2025-2026 : {classeData.Source}"
        });
    }

    private static string BuildParcoursAcademiqueCode(
        short anneeDebut,
        string cycleCode,
        byte niveauNumero,
        string filiereCode,
        string specialiteCode)
        => $"{anneeDebut % 100:00}-{cycleCode}-N{niveauNumero}-{filiereCode}-{specialiteCode}";

    private static string BuildParcoursAcademiqueLibelle(
        string anneeAcademiqueLibelle,
        string cycleCode,
        byte niveauNumero,
        string filiereLibelle,
        string specialiteLibelle)
        => $"{anneeAcademiqueLibelle} - {cycleCode} N{niveauNumero} - {filiereLibelle} - {specialiteLibelle}";

    private static string RequireSpecialiteCode(StudentClassSeedData classe)
    {
        var specialiteCode = NormalizeNullable(classe.SpecialiteCode);

        return specialiteCode
            ?? throw new InvalidOperationException(
                $"La spécialité est obligatoire pour la classe importée '{classe.Code}' ({classe.CycleCode}/{classe.FiliereCode}).");
    }

    private static (string Nom, string Prenoms) SplitNomPrenoms(string value)
    {
        var normalized = Normalize(value);
        var parts = normalized.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return parts.Length switch
        {
            0 => ("Non renseigné", "."),
            1 => (parts[0], "."),
            _ => (parts[0], parts[1])
        };
    }

    private static Sexe ParseSexe(string? value)
    {
        return NormalizeNullable(value)?.ToUpperInvariant() switch
        {
            "M" => Sexe.Masculin,
            "F" => Sexe.Feminin,
            _ => Sexe.NonRenseigne
        };
    }

    private static DateOnly ParseDateOrDefault(string? value, DateOnly defaultValue)
    {
        return DateOnly.TryParse(value, out var date)
            ? date
            : defaultValue;
    }

    private static string Normalize(string value)
        => string.Join(' ', value.Replace('\u00A0', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = Normalize(value);

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed class StudentListsSeedData
    {
        public string AnneeAcademique { get; set; } = string.Empty;
        public List<StudentClassSeedData> Classes { get; set; } = [];
    }

    private sealed class StudentClassSeedData
    {
        public string Source { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public string CycleCode { get; set; } = string.Empty;
        public string FiliereCode { get; set; } = string.Empty;
        public string? SpecialiteCode { get; set; }
        public byte NiveauNumero { get; set; }
        public string? DateInscription { get; set; }
        public List<StudentSeedData> Students { get; set; } = [];
    }

    private sealed class StudentSeedData
    {
        public int? Ordre { get; set; }
        public string? Matricule { get; set; }
        public string NomPrenoms { get; set; } = string.Empty;
        public string? Sexe { get; set; }
        public string? DateNaissance { get; set; }
        public string? LieuNaissance { get; set; }
    }
}
