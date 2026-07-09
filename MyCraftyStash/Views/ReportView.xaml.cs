using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class ReportView : ContentView
{
    private readonly ReportViewModel _vm;

    public ReportView(ReportViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void SetKind(ReportKind kind) => _vm.Init(kind);
}
