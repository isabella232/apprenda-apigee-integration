using System;
using System.Configuration;
using System.Reflection;

namespace ExternalAuthenticationTesting.PlugIn
{
  public class Settings
  {
    private readonly AppSettingsSection _section;

    public Settings()
      : this((string) null)
    {
    }

    internal Settings(string configFileOverride)
    {
      string str = !string.IsNullOrWhiteSpace(configFileOverride) ? configFileOverride : new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath + ".config";
      this._section = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
      {
        ExeConfigFilename = str
      }, ConfigurationUserLevel.None).AppSettings;
    }

    public virtual string AppAlias
    {
      get
      {
        return this.RequiredGet(nameof (AppAlias));
      }
    }

    public virtual string CloudHost
    {
      get
      {
        return this.RequiredGet(nameof (CloudHost));
      }
    }

    public virtual string AppsPart
    {
      get
      {
        return this.RequiredGet(nameof (AppsPart));
      }
    }

    protected string SafeGet(string key)
    {
      KeyValueConfigurationElement setting = this._section.Settings[key];
      return setting != null ? setting.Value : (string) null;
    }

    protected string RequiredGet(string key)
    {
      string str = this.SafeGet(key);
      if (string.IsNullOrWhiteSpace(str))
        throw new ConfigurationErrorsException(string.Format("A value for the required configuration key '{0}' was not provided.", (object) key));
      return str;
    }
  }
}
