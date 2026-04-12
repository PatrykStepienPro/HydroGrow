namespace HydroGrow.Services;

public class PhotoService
{
    public async Task<string?> PickPhotoAsync()
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Wybierz zdjęcie rośliny"
            });

            if (result is null) return null;

            return await CopyToAppStorageAsync(result);
        }
        catch (PermissionException)
        {
            await Shell.Current.DisplayAlert("Brak uprawnień",
                "Przyznaj uprawnienie do galerii w ustawieniach systemu.", "OK");
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<string?> CapturePhotoAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await Shell.Current.DisplayAlert(
                    "Niedostępne",
                    "Aparat nie jest dostępny na tym urządzeniu.",
                    "OK");
                return null;
            }

            var cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
            if (cameraStatus != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert(
                    "Brak uprawnień",
                    "Przyznaj uprawnienie do aparatu w ustawieniach systemu.",
                    "OK");
                return null;
            }

            if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                var storageWriteStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
                if (storageWriteStatus != PermissionStatus.Granted)
                {
                    await Shell.Current.DisplayAlert(
                        "Brak uprawnień",
                        "Przyznaj uprawnienie do zapisu w ustawieniach systemu.",
                        "OK");
                    return null;
                }
            }

            var result = await MediaPicker.Default.CapturePhotoAsync();
            if (result is null)
                return null;

            return await CopyToAppStorageAsync(result);
        }
        catch (PermissionException ex)
        {
            await Shell.Current.DisplayAlert(
                "Błąd uprawnień",
                $"{ex.GetType().Name}: {ex.Message}",
                "OK");
            return null;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                "Błąd",
                $"{ex.GetType().Name}: {ex.Message}",
                "OK");
            return null;
        }
    }

    private static async Task<string?> CopyToAppStorageAsync(FileResult result)
    {
        var photosDir = Constants.PhotosDirectory;
        if (!Directory.Exists(photosDir))
            Directory.CreateDirectory(photosDir);

        var fileName = $"{Guid.NewGuid()}.jpg";
        var destPath = Path.Combine(photosDir, fileName);

        await using var sourceStream = await result.OpenReadAsync();
        await using var destStream = File.OpenWrite(destPath);
        await sourceStream.CopyToAsync(destStream);

        return fileName;
    }

    public string GetFullPath(string relativeFileName) =>
        Path.Combine(Constants.PhotosDirectory, relativeFileName);

    public void DeletePhoto(string relativeFileName)
    {
        try
        {
            var fullPath = GetFullPath(relativeFileName);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch
        {
            // Best-effort deletion
        }
    }
}
