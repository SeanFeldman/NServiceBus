namespace NServiceBus.Features;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Logging;
using Particular.Licensing;

class LicenseReminder : Feature
{
    public LicenseReminder()
    {
        EnableByDefault();

        Defaults(s => s.SetDefault(LicenseTextSettingsKey, null));
        Defaults(s => s.SetDefault(LicenseFilePathSettingsKey, null));
    }

    protected internal override void Setup(FeatureConfigurationContext context)
    {
        try
        {
            var sources = new List<LicenseSource>();

            var settings = context.Settings;

            var licenseTextHasValue = settings.HasExplicitValue(LicenseTextSettingsKey);
            if (licenseTextHasValue)
            {
                var licenseText = settings.Get<string>(LicenseTextSettingsKey);
                if (string.IsNullOrEmpty(licenseText))
                {
                    Logger.Error("Provided license text is null or empty and will not be used as a license source");
                }
                else
                {
                    sources.Add(new LicenseSourceUserProvided(licenseText));
                }
            }

            var licenseHasValue = settings.HasExplicitValue(LicenseFilePathSettingsKey);
            if (licenseHasValue)
            {
                var licensePath = settings.Get<string>(LicenseFilePathSettingsKey);
                if (string.IsNullOrEmpty(licensePath))
                {
                    Logger.Error("Provided license path is null or empty and will not be used as a license source");
                }
                else
                {
                    sources.Add(new LicenseSourceFilePath(licensePath));
                }
            }

            if (!licenseHasValue && !licenseTextHasValue)
            {
                sources = LicenseSource.GetStandardLicenseSources();
            }

            var licenseManager = new LicenseManager();
            licenseManager.InitializeLicense(sources);

            context.Settings.AddStartupDiagnosticsSection("Licensing", GenerateLicenseDiagnostics(licenseManager));

            if (!licenseManager.HasLicenseExpired)
            {
                return;
            }

            context.Pipeline.Register("LicenseReminder", new AuditInvalidLicenseBehavior(), "Audits that the message was processed by an endpoint with an expired license");

            if (Debugger.IsAttached)
            {
                context.Pipeline.Register("LogErrorOnInvalidLicense", new LogErrorOnInvalidLicenseBehavior(), "Logs an error when running in debug mode with an expired license");
            }
        }
        catch (Exception ex)
        {
            //we only log here to prevent licensing issue to abort startup and cause production outages
            Logger.Fatal("Failed to initialize the license", ex);
        }
    }

    static object GenerateLicenseDiagnostics(LicenseManager licenseManager)
    {
        return new
        {
            licenseManager.result.License.RegisteredTo,
            licenseManager.result.License.LicenseType,
            licenseManager.result.License.Edition,
            Tier = licenseManager.result.License.Edition,
            LicenseStatus = licenseManager.result.License.GetLicenseStatus(),
            LicenseLocation = licenseManager.result.Location,
            ValidApplications = string.Join(",", licenseManager.result.License.ValidApplications),
            CommercialLicense = licenseManager.result.License.IsCommercialLicense,
            IsExpired = licenseManager.HasLicenseExpired,
            licenseManager.result.License.ExpirationDate,
            UpgradeProtectionExpirationDate = licenseManager.result.License.UpgradeProtectionExpiration
        };
    }

    public const string LicenseTextSettingsKey = "LicenseText";
    public const string LicenseFilePathSettingsKey = "LicenseFilePath";

    static readonly ILog Logger = LogManager.GetLogger<LicenseReminder>();
}