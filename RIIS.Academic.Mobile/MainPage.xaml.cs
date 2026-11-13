using RIIS.Academic.Mobile.Services;
using System.Text;

namespace RIIS.Academic.Mobile;

public partial class MainPage : ContentPage
{
    private readonly CahierTexteApiClient apiClient = new();
    private FileResult? selectedFile;
    private MobileCaptureUploadResult? uploadedCapture;
    private long? importedCahierTexteId;
    private CahierTexteLookups? lookups;

    public MainPage()
    {
        InitializeComponent();
        ApiUrlEntry.Text = "http://10.0.2.2:5201";
        CourseDatePicker.Date = DateTime.Today;
        StatusLabel.Text = "Renseignez les identifiants des référentiels, puis capturez une page.";
    }

    private async void OnLoadLookupsClicked(object? sender, EventArgs e)
    {
        try
        {
            await RunBusyAsync(async () =>
            {
                lookups = await apiClient.GetLookupsAsync(ApiUrlEntry.Text ?? string.Empty, ApiKeyEntry.Text ?? string.Empty);
                AnneePicker.ItemsSource = lookups.AnneesAcademiques;
                AnneePicker.SelectedIndex = lookups.AnneesAcademiques.Count == 0 ? -1 : 0;
                UpdateClassPicker();
                StatusLabel.Text = $"{lookups.AnneesAcademiques.Count} année(s) et {lookups.Classes.Count} classe(s) chargée(s).";
            });
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception);
        }
    }

    private void OnAnneeSelected(object? sender, EventArgs e)
        => UpdateClassPicker();

    private void UpdateClassPicker()
    {
        if (lookups is null)
            return;

        var annee = AnneePicker.SelectedItem as MobileLookupItem;
        var classes = annee is null
            ? lookups.Classes
            : lookups.Classes.Where(x => x.AnneeAcademiqueId == annee.Id).ToList();

        ClassePicker.ItemsSource = classes.ToList();
        ClassePicker.SelectedIndex = classes.Count == 1 ? 0 : -1;
    }

    private async void OnCaptureClicked(object? sender, EventArgs e)
    {
        try
        {
            var permission = await Permissions.RequestAsync<Permissions.Camera>();
            if (permission != PermissionStatus.Granted)
            {
                await DisplayAlertAsync("Permission requise", "L'accès à la caméra est nécessaire pour scanner le cahier.", "OK");
                return;
            }

            selectedFile = await MediaPicker.Default.CapturePhotoAsync();
            await UploadSelectedFileAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception);
        }
    }

    private async void OnPickImageClicked(object? sender, EventArgs e)
    {
        try
        {
            selectedFile = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choisir une page du cahier",
                FileTypes = FilePickerFileType.Images
            });
            await UploadSelectedFileAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception);
        }
    }

    private async Task UploadSelectedFileAsync()
    {
        if (selectedFile is null)
            return;

        await RunBusyAsync(async () =>
        {
            CapturePreview.Source = ImageSource.FromFile(selectedFile.FullPath);
            CapturePreview.IsVisible = true;
            uploadedCapture = await apiClient.UploadPageAsync(selectedFile, ApiUrlEntry.Text ?? string.Empty, ApiKeyEntry.Text ?? string.Empty);
            ImportButton.IsEnabled = true;
            StatusLabel.Text = $"Page téléversée : {uploadedCapture.OriginalFileName}. Vous pouvez importer les données détectées.";
        });
    }

    private async void OnImportClicked(object? sender, EventArgs e)
    {
        try
        {
            var payload = BuildPayload();
            await RunBusyAsync(async () =>
            {
                var result = await apiClient.ImportAsync(payload, ApiUrlEntry.Text ?? string.Empty, ApiKeyEntry.Text ?? string.Empty);
                importedCahierTexteId = result.CahierTexteId;
                ConfirmButton.IsEnabled = result.EstValide && result.CahierTexteId > 0;
                StatusLabel.Text = BuildImportMessage(result);
            });
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception);
        }
    }

    private async void OnConfirmClicked(object? sender, EventArgs e)
    {
        if (importedCahierTexteId is null)
        {
            await DisplayAlertAsync("Import requis", "Importez d'abord la page du cahier.", "OK");
            return;
        }

        try
        {
            var payload = BuildPayload();
            await RunBusyAsync(async () =>
            {
                var report = await apiClient.ValidateAsync(payload, ApiUrlEntry.Text ?? string.Empty, ApiKeyEntry.Text ?? string.Empty, importedCahierTexteId.Value);
                if (!report.EstValide)
                {
                    StatusLabel.Text = BuildValidationMessage(report);
                    return;
                }

                var result = await apiClient.ConfirmAsync(importedCahierTexteId.Value, payload, ApiUrlEntry.Text ?? string.Empty, ApiKeyEntry.Text ?? string.Empty);
                StatusLabel.Text = $"{result.SeancesEnregistrees} séance(s) confirmée(s) dans RIIS Academic.";
                ConfirmButton.IsEnabled = false;
            });
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception);
        }
    }

    private CahierTexteImportRequest BuildPayload()
    {
        if (uploadedCapture is null)
            throw new InvalidOperationException("Capturez ou choisissez une page avant l'import.");

        var annee = AnneePicker.SelectedItem as MobileLookupItem;
        var classe = ClassePicker.SelectedItem as MobileClassLookupItem;
        if (annee is null || classe is null)
        {
            throw new InvalidOperationException("Sélectionnez une année et une classe liée à un parcours.");
        }

        _ = int.TryParse(PageNumberEntry.Text, out var pageNumber);
        return new CahierTexteImportRequest
        {
            AnneeAcademiqueId = annee.Id,
            ParcoursAcademiqueId = classe.ParcoursAcademiqueId,
            ClassePedagogiqueId = classe.ClassePedagogiqueId,
            EnseignantNom = NullIfEmpty(EnseignantEntry.Text),
            Pages =
            [
                new CahierTextePageImport
                {
                    NumeroPage = pageNumber <= 0 ? 1 : pageNumber,
                    FichierOriginalUri = uploadedCapture.StorageKey,
                    EmpreinteSha256 = uploadedCapture.Sha256,
                    TexteOcr = NullIfEmpty(OcrTextEditor.Text),
                    Seances =
                    [
                        new SeanceCoursImport
                        {
                            OrdreExtraction = 1,
                            DateCours = DateOnly.FromDateTime(CourseDatePicker.Date.GetValueOrDefault()),
                            Nature = "Cours",
                            ElementConstitutifCode = NullIfEmpty(ElementCodeEntry.Text),
                            TitreLecon = NullIfEmpty(LessonTitleEntry.Text),
                            Paragraphe = NullIfEmpty(OcrTextEditor.Text)
                        }
                    ]
                }
            ]
        };
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        BusyIndicator.IsVisible = true;
        BusyIndicator.IsRunning = true;
        try
        {
            await action();
        }
        finally
        {
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildImportMessage(CahierTexteImportResult result)
    {
        var seances = result.Validation?.Seances.Count ?? 0;
        var message = new StringBuilder().Append($"{result.PagesImportees} page(s) importée(s), {seances} séance(s) détectée(s).");
        if (result.Issues.Count > 0)
            message.Append(" Informations : ").Append(string.Join(" | ", result.Issues.Select(x => x.Message)));
        return message.ToString();
    }

    private static string BuildValidationMessage(CahierTexteValidationReport report)
        => string.Join("\n", report.Issues.Select(issue => $"[{issue.Severity}] {issue.Message}")
            .Concat(report.Seances.SelectMany(seance => seance.Issues)
                .Select(issue => $"[{issue.Severity}] {issue.Message}")));

    private async Task ShowErrorAsync(Exception exception)
        => await DisplayAlertAsync("Erreur", exception.Message, "OK");
}
