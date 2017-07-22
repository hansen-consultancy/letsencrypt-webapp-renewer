﻿using System;
using System.Diagnostics;
using OhadSoft.AzureLetsEncrypt.Renewal.Management;
using OhadSoft.AzureLetsEncrypt.Renewal.WebJob.AppSettings;
using OhadSoft.AzureLetsEncrypt.Renewal.WebJob.CLI;

namespace OhadSoft.AzureLetsEncrypt.Renewal.WebJob
{
    internal static class Program
    {
        private const int Success = 0;
        private const int ArgumentError = 1;

        private static int Main(string[] args)
        {
            var webjobName = Environment.GetEnvironmentVariable("WEBJOBS_NAME");
            return webjobName != null ? WebJobMain(webjobName) : CliMain(args);
        }

        private static int WebJobMain(string webjobName)
        {
            Trace.TraceInformation("Web App SSL renewal job ({0}) started", webjobName);
            var renewr = new AppSettingsRenewer(new CertRenewer(new RenewalManager()), new AppSettingsRenewalParamsReader(new AppSettingsReader()));
            try
            {
                renewr.Renew();
            }
            catch (Exception e)
            {
                Trace.TraceError("Unexpected exception: {0}", e);
                throw; // we want the webjob to fail
            }

            return Success;
        }

        private static int CliMain(string[] args)
        {
            Trace.TraceInformation("Web App SSL renewal CLI started, parameters: {0}", string.Join(", ", args));
            var renewer = new CliRenewer(new CertRenewer(new RenewalManager()), new CommandlineRenewalParamsReader());

            try
            {
                renewer.Renew(args);
            }
            catch (ArgumentException e)
            {
                Trace.TraceError("Error parsing arguments: {0}", e);
                PrintUsage();
                return ArgumentError;
            }
            catch (Exception e)
            {
                Trace.TraceError("Unexpected exception: {0}", e);
                throw;
            }

            return Success;
        }

        private static void PrintUsage()
        {
            Trace.TraceInformation(
                "Usage: {0}.exe SubscriptionId TenantId ResourceGroup WebApp Hosts Email ClientId ClientSecret [UseIpBasedSsl] [RsaKeyLength] [AcmeBasedUri]",
                typeof(Program).Assembly.GetName().Name);
            Trace.TraceInformation("'Hosts' is a semicolon-delimited list of host names");
            Trace.TraceInformation("'UseIpBasedSsl' is optional and defaults to false");
            Trace.TraceInformation("'RsaKeyLength' is optional and defaults to 2048");
            Trace.TraceInformation("'AcmeBasedUri' is optional and defaults to https://acme-v01.api.letsencrypt.org/");
            Trace.TraceInformation("Consult the Let's Encrypt documentation for rate limits: https://letsencrypt.org/docs/rate-limits/");
            Trace.TraceInformation("Exit codes: {0} = success, {1} = argument error (any other error will crash the process)", Success, ArgumentError);
        }
    }
}