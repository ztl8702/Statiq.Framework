﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Options;
using Statiq.Common;

namespace Statiq.App
{
    public static class BootstrapperDefaultExtensions
    {
        public static Bootstrapper AddDefaults(this Bootstrapper bootstrapper, DefaultFeatures features = DefaultFeatures.All)
        {
            _ = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
            if (features.HasFlag(DefaultFeatures.BootstrapperConfigurators))
            {
                bootstrapper.AddBootstrapperConfigurators();
            }
            if (features.HasFlag(DefaultFeatures.Logging))
            {
                bootstrapper.AddDefaultLogging();
            }
            if (features.HasFlag(DefaultFeatures.Settings))
            {
                bootstrapper.AddDefaultSettings();
            }
            if (features.HasFlag(DefaultFeatures.EnvironmentVariables))
            {
                bootstrapper.AddEnvironmentVariables();
            }
            if (features.HasFlag(DefaultFeatures.ConfigurationFiles))
            {
                bootstrapper.AddDefaultConfigurationFiles();
            }
            if (features.HasFlag(DefaultFeatures.BuildCommands))
            {
                bootstrapper.AddBuildCommands();
            }
            if (features.HasFlag(DefaultFeatures.HostingCommands))
            {
                bootstrapper.AddHostingCommands();
            }
            if (features.HasFlag(DefaultFeatures.CustomCommands))
            {
                bootstrapper.AddCustomCommands();
            }
            if (features.HasFlag(DefaultFeatures.Shortcodes))
            {
                bootstrapper.AddDefaultShortcodes();
            }
            if (features.HasFlag(DefaultFeatures.Namespaces))
            {
                bootstrapper.AddDefaultNamespaces();
            }
            if (features.HasFlag(DefaultFeatures.Pipelines))
            {
                bootstrapper.AddDefaultPipelines();
            }
            return bootstrapper;
        }

        public static Bootstrapper AddDefaultsWithout(this Bootstrapper bootstrapper, DefaultFeatures withoutFeatures) =>
            bootstrapper.AddDefaults(DefaultFeatures.All & ~withoutFeatures);

        public static Bootstrapper AddBootstrapperConfigurators(this Bootstrapper bootstrapper)
        {
            _ = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
            foreach (IConfigurator<IConfigurableBootstrapper> bootstraperConfigurator
                in bootstrapper.ClassCatalog.GetInstances<IConfigurator<IConfigurableBootstrapper>>())
            {
                bootstrapper.Configurators.Add(bootstraperConfigurator);
            }
            foreach (IConfigurator<Bootstrapper> bootstraperConfigurator
                in bootstrapper.ClassCatalog.GetInstances<IConfigurator<Bootstrapper>>())
            {
                bootstrapper.Configurators.Add(bootstraperConfigurator);
            }
            return bootstrapper;
        }

        public static Bootstrapper AddDefaultLogging(this Bootstrapper bootstrapper) =>
            bootstrapper.ConfigureServices(services =>
            {
                services.AddSingleton<ILoggerProvider, ConsoleLoggerProvider>();
                services.AddLogging(logging => logging.AddDebug());
            });

        public static Bootstrapper AddDefaultSettings(this Bootstrapper bootstrapper) =>
            bootstrapper.AddSettingsIfNonExisting(
                new Dictionary<string, string>
                {
                    { Keys.LinkHideIndexPages, "true" },
                    { Keys.LinkHideExtensions, "true" },
                    { Keys.UseCache, "true" },
                    { Keys.CleanOutputPath, "true" }
                });

        public static Bootstrapper AddEnvironmentVariables(this Bootstrapper bootstrapper) =>
            bootstrapper.BuildConfiguration(builder => builder.AddEnvironmentVariables());

        public static Bootstrapper AddDefaultConfigurationFiles(this Bootstrapper bootstrapper) =>
            bootstrapper.BuildConfiguration(builder => builder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("statiq.json", true));

        public static Bootstrapper AddBuildCommands(this Bootstrapper bootstrapper)
        {
            _ = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
            bootstrapper.SetDefaultCommand<PipelinesCommand<PipelinesCommandSettings>>();
            bootstrapper.AddCommand<PipelinesCommand<PipelinesCommandSettings>>();
            bootstrapper.AddCommand<DeployCommand>();
            bootstrapper.AddCommands();
            return bootstrapper;
        }

        public static Bootstrapper AddHostingCommands(this Bootstrapper bootstrapper)
        {
            _ = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
            bootstrapper.AddCommand<PreviewCommand>();
            bootstrapper.AddCommand<ServeCommand>();
            return bootstrapper;
        }

        public static Bootstrapper AddCustomCommands(this Bootstrapper bootstrapper) => bootstrapper.AddCommands();

        public static Bootstrapper AddDefaultShortcodes(this Bootstrapper bootstrapper) =>
            bootstrapper.ConfigureEngine(engine =>
            {
                foreach (Type shortcode in bootstrapper.ClassCatalog.GetTypesAssignableTo<IShortcode>())
                {
                    engine.Shortcodes.Add(shortcode);

                    // Special case for the meta shortcode to register with the name "="
                    if (shortcode.Equals(typeof(Core.MetaShortcode)))
                    {
                        engine.Shortcodes.Add("=", shortcode);
                    }
                }
            });

        public static Bootstrapper AddDefaultNamespaces(this Bootstrapper bootstrapper) =>
            bootstrapper.ConfigureEngine(engine =>
            {
                // Add all Statiq.Common namespaces
                // the JetBrains.Profiler filter is needed due to DotTrace dynamically
                // adding a reference to that assembly when running under its profiler. We want
                // to exclude it.
                engine.Namespaces.AddRange(typeof(IModule).Assembly.GetTypes()
                    .Where(x => !string.IsNullOrWhiteSpace(x.Namespace) && !x.Namespace.StartsWith("JetBrains.Profiler"))
                    .Select(x => x.Namespace)
                    .Distinct());

                // Add all module namespaces
                engine.Namespaces.AddRange(
                    bootstrapper.ClassCatalog
                        .GetTypesAssignableTo<IModule>()
                        .Select(x => x.Namespace));

                // Add all namespaces from the entry app
                Assembly entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    engine.Namespaces.AddRange(
                        bootstrapper.ClassCatalog
                            .GetTypesFromAssembly(entryAssembly)
                            .Select(x => x.Namespace)
                            .Distinct());
                }
            });

        public static Bootstrapper AddDefaultPipelines(this Bootstrapper bootstrapper) => bootstrapper.AddPipelines();
    }
}
