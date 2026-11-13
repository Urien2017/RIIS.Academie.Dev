using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Application.Authorization.Services;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class AuthorizationSeeder
{
    private static readonly (string Code, string Libelle)[] BaseRoles =
    [
        ("ADMIN", "Admin"),
        ("DIRECTEUR", "Directeur"),
        ("DAAC", "Daac"),
        ("CAISSE", "Caisse"),
        ("SAISIE", "Saisie")
    ];

    public static async Task SeedAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        var roles = new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase);

        foreach (var (code, libelle) in BaseRoles)
        {
            var role = await context.Roles.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
            if (role is null)
            {
                role = new Role
                {
                    Code = code,
                    Libelle = libelle,
                    EstSysteme = true,
                    EstActif = true
                };
                context.Roles.Add(role);
            }
            else
            {
                role.Libelle = libelle;
                role.EstSysteme = true;
                role.EstActif = true;
            }

            roles[code] = role;
        }

        await context.SaveChangesAsync(cancellationToken);

        await EnsureMenuPermissionsAsync(context, cancellationToken);

        var adminRole = roles["ADMIN"];
        var admin = await context.Utilisateurs
            .FirstOrDefaultAsync(x => x.NomUtilisateur == "admin", cancellationToken);

        if (admin is null)
        {
            context.Utilisateurs.Add(new Utilisateur
            {
                NomUtilisateur = "admin",
                MotDePasseHash = MotDePasseHasher.Hash("admin"),
                RoleId = adminRole.Id,
                EstActif = true
            });
        }
        else
        {
            admin.RoleId = adminRole.Id;
            admin.EstActif = true;
            if (!MotDePasseHasher.Verify("admin", admin.MotDePasseHash))
            {
                admin.MotDePasseHash = MotDePasseHasher.Hash("admin");
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureMenuPermissionsAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new MenuPermission("/", "Tableau de bord", "/", "insights", null, 1),
            new MenuPermission("/dashboard-scolarite", "Dashboard scolarité", "/dashboard-scolarite", "payments", null, 2),
            new MenuPermission("referentiels", "Référentiels", null, "settings", null, 10),
            new MenuPermission("referentiels/annees", "Année académique", "/referentiels/annees", "calendar_month", "referentiels", 1),
            new MenuPermission("referentiels/cycles", "Cycle", "/referentiels/cycles", "account_tree", "referentiels", 2),
            new MenuPermission("referentiels/filieres", "Filière", "/referentiels/filieres", "category", "referentiels", 3),
            new MenuPermission("referentiels/specialites", "Spécialité", "/referentiels/specialites", "workspace_premium", "referentiels", 4),
            new MenuPermission("referentiels/niveaux", "Niveau", "/referentiels/niveaux", "stairs", "referentiels", 5),
            new MenuPermission("referentiels/parcours-academiques", "Parcours académiques", "/referentiels/parcours-academiques", "route", "referentiels", 6),
            new MenuPermission("classes-pedagogiques", "Classes", "/classes-pedagogiques", "co_present", "referentiels", 7),
            new MenuPermission("programme", "Programme pédagogique", null, "menu_book", null, 20),
            new MenuPermission("programme/maquettes", "Maquette", "/programme/maquettes", "schema", "programme", 1),
            new MenuPermission("programme/affectations-maquettes", "Affectations des maquettes", "/programme/affectations-maquettes", "link", "programme", 2),
            new MenuPermission("programme/semestres", "Semestre", "/programme/semestres", "view_timeline", "programme", 3),
            new MenuPermission("programme/ue", "UE", "/programme/ue", "view_module", "programme", 4),
            new MenuPermission("programme/ec", "EC", "/programme/ec", "article", "programme", 5),
            new MenuPermission("academique", "Académique", null, "school", null, 30),
            new MenuPermission("etudiants", "Étudiants", "/etudiants", "groups", "academique", 1),
            new MenuPermission("etudiants/dossier", "Dossier étudiant", "/etudiants/dossier", "folder_shared", "academique", 2),
            new MenuPermission("evaluations", "Évaluations", null, "fact_check", null, 40),
            new MenuPermission("saisie-notes", "Saisie des notes", "/saisie-notes", "edit_note", "evaluations", 1),
            new MenuPermission("/evaluations", "Résultats", "/evaluations", "analytics", "evaluations", 2),
            new MenuPermission("calcul-resultats", "Calcul des résultats", "/calcul-resultats", "calculate", "evaluations", 3),
            new MenuPermission("pv", "PV", "/pv", "article", "evaluations", 4),
            new MenuPermission("releves", "Relevés", "/releves", "description", "evaluations", 5),
            new MenuPermission("scolarite", "Scolarité", null, "payments", null, 50),
            new MenuPermission("scolarite/types-elements", "Types d'éléments", "/scolarite/types-elements", "tune", "scolarite", 1),
            new MenuPermission("scolarite/tarifs", "Tarifs", "/scolarite/tarifs", "price_change", "scolarite", 2),
            new MenuPermission("scolarite/dossiers", "Dossiers", "/scolarite/dossiers", "folder_open", "scolarite", 3),
            new MenuPermission("scolarite/finances", "Finances", "/scolarite/finances", "payments", "scolarite", 4),
            new MenuPermission("scolarite/modes-paiement", "Modes de paiement", "/scolarite/modes-paiement", "account_balance_wallet", "scolarite", 5),
            new MenuPermission("/Dashboard-docs", "Menu détaillé", "/Dashboard-docs", "dashboard", null, 60),
            new MenuPermission("administration", "Administration", null, "admin_panel_settings", null, 70),
            new MenuPermission("administration/roles", "Gestion des rôles", "/administration/roles", "manage_accounts", "administration", 1),
            new MenuPermission("administration/users", "Utilisateurs", "/administration/users", "group", "administration", 2)
        };

        var existing = await context.Permissions.ToListAsync(cancellationToken);
        var byCode = existing.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            if (!byCode.TryGetValue(definition.Code, out var permission))
            {
                permission = new Permission { Code = definition.Code };
                context.Permissions.Add(permission);
                byCode[definition.Code] = permission;
            }

            permission.Libelle = definition.Libelle;
            permission.Route = definition.Route;
            permission.Icon = definition.Icon;
            permission.OrdreAffichage = definition.Ordre;
            permission.EstActif = true;
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var definition in definitions)
        {
            byCode[definition.Code].ParentPermissionId =
                definition.ParentCode is null ? null : byCode[definition.ParentCode].Id;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private sealed record MenuPermission(
        string Code,
        string Libelle,
        string? Route,
        string Icon,
        string? ParentCode,
        int Ordre);
}
