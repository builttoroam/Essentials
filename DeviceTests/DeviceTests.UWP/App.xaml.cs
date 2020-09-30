using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Graphics.Canvas.Effects;
using UnitTests.HeadlessRunner;
using Windows.ApplicationModel.Activation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Xunit.Runners.UI;

namespace DeviceTests.UWP
{
    public sealed partial class App : RunnerApplication
    {
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);

            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                Window.Current.Content = rootFrame;
            }

            // Ensure the current window is active
            Window.Current.Activate();

            if (args.Kind == ActivationKind.Protocol)
            {
                var protocolArgs = (ProtocolActivatedEventArgs)args;
                if (!string.IsNullOrEmpty(protocolArgs?.Uri?.Host))
                {
                    var parts = protocolArgs.Uri.Host.Split('_');
                    if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]))
                    {
                        var ip = parts[0]?.Replace('-', '.');

                        if (int.TryParse(parts[1], out var port))
                        {
                            await Tests.RunAsync(new TestOptions
                            {
                                Assemblies = new List<Assembly> { typeof(Battery_Tests).Assembly },
                                NetworkLogHost = ip,
                                NetworkLogPort = port,
                                Filters = Traits.GetCommonTraits(),
                                Format = TestResultsFormat.XunitV2
                            });
                        }
                    }
                }
            }
        }

        /*
            if (args.Kind == ActivationKind.Protocol)
            {
                var protocolArgs = (ProtocolActivatedEventArgs)args;
                if (!string.IsNullOrEmpty(protocolArgs?.Uri?.Host))
                {
                    var parts = protocolArgs.Uri.Host.Split('_');
                    if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]))
                    {
                        var ip = parts[0]?.Replace('-', '.');

                        if (int.TryParse(parts[1], out var port))
                        {
                            Tests.RunAsync(new TestOptions
                            {
                                Assemblies = new List<Assembly> { typeof(Battery_Tests).Assembly },
                                NetworkLogHost = ip,
                                NetworkLogPort = port,
                                Filters = Traits.GetCommonTraits(),
                                Format = TestResultsFormat.XunitV2
                            });

                            Task.Run(() =>
                            {
                                var xunitRunner = new UnitTests.HeadlessRunner.Xunit.XUnitTestInstrumentation
                                {
                                    NetworkLogEnabled = true,
                                    NetworkLogHost = ip,
                                    NetworkLogPort = port,
                                    ResultsFormat = TestResultsFormat.XunitV2,
                                    Filters = Traits.GetCommonTraits()
                                };

                                var asm = typeof(App).GetTypeInfo().Assembly;
                                var asmFilename = asm.GetName().Name + ".exe";

                                var tests = typeof(Accelerometer_Tests).GetTypeInfo().Assembly;
                                var testsFilename = tests.GetName().Name + ".dll";

                                xunitRunner.Run(new TestAssemblyInfo(asm, asmFilename), new TestAssemblyInfo(tests, testsFilename));
                            });
    }
}
                }
            }
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            // if (args.Kind == ActivationKind.Protocol)
            // {
            // var protocolArgs = (ProtocolActivatedEventArgs)args;
            var url = "192-168-1-108_63559";
            if (!string.IsNullOrEmpty(url))
            {
                var parts = url.Split('_');
                if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]))
                {
                    var ip = parts[0]?.Replace('-', '.');

                    if (int.TryParse(parts[1], out var port))
                    {
                        Task.Run(() =>
                        {
                            var xunitRunner = new UnitTests.HeadlessRunner.Xunit.XUnitTestInstrumentation
                            {
                                NetworkLogEnabled = true,
                                NetworkLogHost = ip,
                                NetworkLogPort = port,
                                ResultsFormat = TestResultsFormat.XunitV2,
                                Filters = Traits.GetCommonTraits()
                            };

                            var asm = typeof(App).GetTypeInfo().Assembly;
                            var asmFilename = asm.GetName().Name + ".exe";

                            var tests = typeof(Accelerometer_Tests).GetTypeInfo().Assembly;
                            var testsFilename = tests.GetName().Name + ".dll";

                            xunitRunner.Run(new TestAssemblyInfo(asm, asmFilename), new TestAssemblyInfo(tests, testsFilename));
                        });
                    }
                }
            }

            // }

            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                Window.Current.Content = rootFrame;
            }

            // Ensure the current window is active
            Window.Current.Activate();

            // base.OnLaunched(e);
        }
        */

        protected override void OnInitializeRunner()
        {
            AddTestAssembly(typeof(App).GetTypeInfo().Assembly);
            AddTestAssembly(typeof(Accelerometer_Tests).GetTypeInfo().Assembly);
        }
    }
}
