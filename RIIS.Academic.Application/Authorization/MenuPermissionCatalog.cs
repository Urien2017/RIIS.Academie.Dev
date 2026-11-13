namespace RIIS.Academic.Application.Authorization;

public sealed record MenuPermissionDefinition(
    string Code,
    string Libelle,
    string? Route,
    string Icon,
    string? ParentCode,
    int Ordre);

public static class MenuPermissionCatalog
{
    public static IReadOnlyList<MenuPermissionDefinition> All { get; } =
    [
        new("/", "Tableau de bord", "/", "insights", null, 1),
        new("/dashboard-scolarite", "Dashboard scolarité", "/dashboard-scolarite", "payments", null, 2),
        new("referentiels", "Référentiels", null, "settings", null, 10),
        new("referentiels/annees", "Année académique", "/referentiels/annees", "calendar_month", "referentiels", 1),
        new("referentiels/cycles", "Cycle", "/referentiels/cycles", "account_tree", "referentiels", 2),
        new("referentiels/filieres", "Filière", "/referentiels/filieres", "category", "referentiels", 3),
        new("referentiels/specialites", "Spécialité", "/referentiels/specialites", "workspace_premium", "referentiels", 4),
        new("referentiels/niveaux", "Niveau", "/referentiels/niveaux", "stairs", "referentiels", 5),
        new("referentiels/parcours-academiques", "Parcours académiques", "/referentiels/parcours-academiques", "route", "referentiels", 6),
        new("classes-pedagogiques", "Classes", "/classes-pedagogiques", "co_present", "referentiels", 7),
        new("programme", "Programme pédagogique", null, "menu_book", null, 20),
        new("programme/maquettes", "Maquette", "/programme/maquettes", "schema", "programme", 1),
        new("programme/semestres", "Semestre", "/programme/semestres", "view_timeline", "programme", 2),
        new("programme/ue", "UE", "/programme/ue", "view_module", "programme", 3),
        new("programme/ec", "EC", "/programme/ec", "article", "programme", 4),
        new("academique", "Académique", null, "school", null, 30),
        new("etudiants", "Étudiants", "/etudiants", "groups", "academique", 1),
        new("etudiants/dossier", "Dossier étudiant", "/etudiants/dossier", "folder_shared", "academique", 2),
        new("evaluations", "Évaluations", null, "fact_check", null, 40),
        new("saisie-notes", "Saisie des notes", "/saisie-notes", "edit_note", "evaluations", 1),
        new("/evaluations", "Résultats", "/evaluations", "analytics", "evaluations", 2),
        new("calcul-resultats", "Calcul des résultats", "/calcul-resultats", "calculate", "evaluations", 3),
        new("pv", "PV", "/pv", "article", "evaluations", 4),
        new("releves", "Relevés", "/releves", "description", "evaluations", 5),
        new("scolarite", "Scolarité", null, "payments", null, 50),
        new("scolarite/types-elements", "Types d'éléments", "/scolarite/types-elements", "tune", "scolarite", 1),
        new("scolarite/tarifs", "Tarifs", "/scolarite/tarifs", "price_change", "scolarite", 2),
        new("scolarite/dossiers", "Dossiers", "/scolarite/dossiers", "folder_open", "scolarite", 3),
        new("scolarite/finances", "Finances", "/scolarite/finances", "payments", "scolarite", 4),
        new("scolarite/modes-paiement", "Modes de paiement", "/scolarite/modes-paiement", "account_balance_wallet", "scolarite", 5),
        new("/Dashboard-docs", "Menu détaillé", "/Dashboard-docs", "dashboard", null, 60),
        new("administration", "Administration", null, "admin_panel_settings", null, 70),
        new("administration/roles", "Gestion des rôles", "/administration/roles", "manage_accounts", "administration", 1),
        new("administration/users", "Utilisateurs", "/administration/users", "group", "administration", 2)
    ];
}
