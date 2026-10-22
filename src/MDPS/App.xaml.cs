using System.IO;
using System.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MDPS;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show("O MDPS encontrou um erro: " + args.Exception.Message, "MDPS");
            args.Handled = true;
            Shutdown(1);
        };
        var window = new MainWindow(e.Args);
        MainWindow = window;
        window.Show();
    }
}
