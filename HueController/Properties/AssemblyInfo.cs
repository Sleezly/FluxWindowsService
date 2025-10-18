using System.Runtime.CompilerServices;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config")]
[assembly: InternalsVisibleTo("HueController.UnitTests")]