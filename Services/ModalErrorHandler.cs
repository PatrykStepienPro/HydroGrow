namespace HydroGrow.Services;

/// <summary>
/// Modal Error Handler.
/// </summary>
public class ModalErrorHandler : IErrorHandler
{
	SemaphoreSlim _semaphore = new(1, 1);
	readonly ILogger<ModalErrorHandler> _logger;

	public ModalErrorHandler(ILogger<ModalErrorHandler> logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// Handle error in UI.
	/// </summary>
	/// <param name="ex">Exception.</param>
	public void HandleError(Exception ex)
	{
		_logger.LogError(ex, "Handled error: {Message}", ex.Message);
		DisplayAlertAsync(ex).FireAndForgetSafeAsync();
	}

	async Task DisplayAlertAsync(Exception ex)
	{
		try{
			await _semaphore.WaitAsync();
			if (Shell.Current is Shell shell)
				await shell.DisplayAlertAsync("Error", ex.Message, "OK");
		}
		finally{
			_semaphore.Release();
		}
	}
}