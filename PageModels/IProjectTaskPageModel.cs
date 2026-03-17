using CommunityToolkit.Mvvm.Input;
using HydroGrow.Models;

namespace HydroGrow.PageModels;

public interface IProjectTaskPageModel
{
	IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
	bool IsBusy { get; }
}