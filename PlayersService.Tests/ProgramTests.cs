﻿using System;
using System.IO;
using log4net;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.PlayersService.Tests
{
    class ProgramTests
    {
        [TestClass]
        public class MainImpl
        {
            public MainImpl()
            {
                log = mockLog.Object;
                environment = mockEnvironment.Object;
                parser = mockParser.Object;
                application = mockApplication.Object;

                TelemetryConfiguration.Active.InstrumentationKey = "";
            }

            Mock<ILog> mockLog = new Mock<ILog>();
            ILog log;
            Mock<IEnvironment> mockEnvironment = new Mock<IEnvironment>();
            IEnvironment environment;
            Mock<IArgsParser<IPlayersSettings>> mockParser = new Mock<IArgsParser<IPlayersSettings>>();
            IArgsParser<IPlayersSettings> parser;
            Mock<IApplication> mockApplication = new Mock<IApplication>();
            IApplication application;

            [TestMethod]
            public void InitializesLogging()
            {
                // Arrange
                string[] args = new string[0];
                IPlayersSettings settings = new SimplePlayersSettings();

                // Act
                Program.MainImpl(args, log, environment, parser, settings, application);

                // Assert
                mockLog.Verify(l => l.Debug("Initialized logging."));
            }

            [TestMethod]
            public void SetsCurrentDirectoryToBaseDirectory()
            {
                // Arrange
                string[] args = new string[0];
                IPlayersSettings settings = new SimplePlayersSettings();

                // Act
                Program.MainImpl(args, log, environment, parser, settings, application);

                // Assert
                Assert.AreEqual(AppDomain.CurrentDomain.BaseDirectory, Directory.GetCurrentDirectory());
            }

            [TestMethod]
            public void ArgsIsEmpty_DoesNotCallParse()
            {
                // Arrange
                string[] args = new string[0];
                IPlayersSettings settings = new SimplePlayersSettings();

                // Act
                Program.MainImpl(args, log, environment, parser, settings, application);

                // Assert
                mockParser.Verify(p => p.Parse(It.IsAny<string[]>(), It.IsAny<IPlayersSettings>()), Times.Never);
            }

            [TestMethod]
            public void ArgsIsNotEmptyAndUserInteractiveIsFalse_DoesNotCallParse()
            {
                // Arrange
                string[] args = new[] { "--myArg" };
                IPlayersSettings settings = new SimplePlayersSettings();
                mockEnvironment
                    .SetupGet(e => e.UserInteractive)
                    .Returns(false);

                // Act
                Program.MainImpl(args, log, environment, parser, settings, application);

                // Assert
                mockParser.Verify(p => p.Parse(It.IsAny<string[]>(), It.IsAny<IPlayersSettings>()), Times.Never);
            }

            [TestMethod]
            public void ArgsIsNotEmptyAndUserInteractiveIsTrue_CallsParse()
            {
                // Arrange
                string[] args = new[] { "--myArg" };
                IPlayersSettings settings = new SimplePlayersSettings();
                mockEnvironment
                    .SetupGet(e => e.UserInteractive)
                    .Returns(true);

                // Act
                Program.MainImpl(args, log, environment, parser, settings, application);

                // Assert
                mockParser.Verify(p => p.Parse(It.IsAny<string[]>(), It.IsAny<IPlayersSettings>()), Times.Once);
            }

            [TestMethod]
            public void ArgsIsNotEmptyAndUserInteractiveIsTrue_ReturnsExitCodeFromParse()
            {
                // Arrange
                string[] args = new[] { "--myArg" };
                IPlayersSettings settings = new SimplePlayersSettings();
                mockEnvironment
                    .SetupGet(e => e.UserInteractive)
                    .Returns(true);
                mockParser
                    .Setup(p => p.Parse(It.IsAny<string[]>(), It.IsAny<IPlayersSettings>()))
                    .Returns(20);

                // Act
                var ret = Program.MainImpl(args, log, environment, parser, settings, application);

                // Assert
                Assert.AreEqual(20, ret);
            }

            [TestMethod]
            public void InstrumentationKeyIsNull_LogsWarning()
            {
                // Arrange
                string[] args = new string[0];
                IPlayersSettings settings = new SimplePlayersSettings { InstrumentationKey = null };

                // Act
                Program.MainImpl(args, log, environment, parser, settings, application);

                // Assert
                mockLog.Verify(l => l.Warn("The setting 'InstrumentationKey' is not set. Telemetry is disabled."));
            }

            [TestMethod]
            public void InstrumentationKeyIsEmpty_LogsWarning()
            {
                // Arrange
                string[] args = new string[0];
                IPlayersSettings settings = new SimplePlayersSettings { InstrumentationKey = "" };

                // Act
                Program.MainImpl(args, log, environment, parser, settings, application);

                // Assert
                mockLog.Verify(l => l.Warn("The setting 'InstrumentationKey' is not set. Telemetry is disabled."));
            }

            [TestMethod]
            public void InstrumentationKeyIsSet_SetsInstrumentationKeyForTelemetry()
            {
                // Arrange
                string[] args = new string[0];
                IPlayersSettings settings = new SimplePlayersSettings { InstrumentationKey = "myInstrumentationKey" };

                // Act
                Program.MainImpl(args, log, environment, parser, settings, application);

                // Assert
                Assert.AreEqual("myInstrumentationKey", TelemetryConfiguration.Active.InstrumentationKey);
            }

            [TestMethod]
            public void CallsApplicationRun()
            {
                // Arrange
                string[] args = new string[0];
                IPlayersSettings settings = new SimplePlayersSettings();

                // Act
                Program.MainImpl(args, log, environment, parser, settings, application);

                // Assert
                mockApplication.Verify(a => a.Run<WorkerRole, IPlayersSettings>());
            }

            [TestMethod]
            public void Returns0()
            {
                // Arrange
                string[] args = new string[0];
                IPlayersSettings settings = new SimplePlayersSettings();

                // Act
                var ret = Program.MainImpl(args, log, environment, parser, settings, application);

                // Assert
                Assert.AreEqual(0, ret);
            }
        }
    }
}
