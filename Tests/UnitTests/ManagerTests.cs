﻿using System;
using Models;
using Models.Core;
using APSIM.Shared.Utilities;
using NUnit.Framework;
namespace UnitTests
{
    using System.Collections.Generic;
    using Models.Core.ApsimFile;
    using Models.Storage;
    using System.IO;
    using APSIM.Shared.JobRunning;
    using Models.Core.Run;
    using UnitTests.Storage;

    /// <summary>
    /// Unit Tests for manager scripts.
    /// </summary>
    class ManagerTests
    {
        /// <summary>
        /// This test reproduces a bug in which a simulation could run without
        /// error despite a manager script containing a syntax error.
        /// </summary>
        [Test]
        public void TestManagerWithError()
        {
            var simulations = new Simulations()
            { 
                Children = new List<IModel>()
                {
                    new Simulation()
                    {
                        Name = "Sim",
                        FileName = Path.GetTempFileName(),
                        Children = new List<IModel>()
                        {
                            new Clock()
                            {
                                StartDate = new DateTime(2019, 1, 1),
                                EndDate = new DateTime(2019, 1, 2)
                            },
                            new MockSummary(),
                            new Manager()
                            {
                                Code = "asdf"
                            }
                        }
                    }
                }
            };
            Apsim.ParentAllChildren(simulations);

            var runner = new Runner(simulations);
            Assert.IsNotNull(runner.Run());
        }

        /// <summary>
        /// This test ensures that scripts aren't recompiled after events have
        /// been hooked up. Such behaviour would cause scripts to not receive
        /// any events, and the old/discarded scripts would receive events.
        /// </summary>
        [Test]
        public void TestScriptNotRebuilt()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.bork.apsimx");
            IModel file = FileFormat.ReadFromString<IModel>(json, out List<Exception> errors);
            Simulation sim = Apsim.Find(file, typeof(Simulation)) as Simulation;
            Assert.DoesNotThrow(() => sim.Run());
        }

        /// <summary>
        /// Ensures that Manager Scripts are allowed to override the
        /// OnCreated() method.
        /// </summary>
        /// <remarks>
        /// OnCreatedError.apsimx contains a manager script which overrides
        /// the OnCreated() method and throws an exception from this method.
        /// 
        /// This test ensures that an exception is thrown and that it is the
        /// correct exception.
        /// 
        /// The manager in this file is disabled, but its OnCreated() method
        /// should still be called.
        /// </remarks>
        [Test]
        public void ManagerScriptOnCreated()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.OnCreatedError.apsimx");
            List<Exception> errors = new List<Exception>();
            FileFormat.ReadFromString<IModel>(json, out errors);

            Assert.NotNull(errors);
            Assert.AreEqual(1, errors.Count, "Encountered the wrong number of errors when opening OnCreatedError.apsimx.");
            Assert.That(errors[0].ToString().Contains("Error thrown from manager script's OnCreated()"), "Encountered an error while opening OnCreatedError.apsimx, but it appears to be the wrong error: {0}.", errors[0].ToString());
        }

        /// <summary>
        /// This test ensures one manager model can call another.
        /// </summary>
        [Test]
        public void TestOneManagerCallingAnother()
        {
            var simulation = new Simulation()
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 1)},
                    new MockSummary(),
                    new MockStorage(),
                    new Manager()
                    {
                        Name = "Manager1",
                        Code = "using Models.Core;" + Environment.NewLine +
                                "namespace Models" + Environment.NewLine +
                                "{" + Environment.NewLine +
                                "    public class Script1 : Model" + Environment.NewLine +
                                "    {" + Environment.NewLine +
                                "        public int A = 1;" + Environment.NewLine +
                                "    }" + Environment.NewLine +
                                "}"
                    },
                    new Manager()
                    {
                        Name = "Manager2",
                        Code = "using Models.Core;" + Environment.NewLine +
                                "namespace Models" + Environment.NewLine +
                                "{" + Environment.NewLine +
                                "    public class Script2 : Model" + Environment.NewLine +
                                "    {" + Environment.NewLine +
                                "        [Link] Script1 otherScript;" + Environment.NewLine +
                                "        public int B { get { return otherScript.A + 1; } }" + Environment.NewLine +
                                "    }" + Environment.NewLine +
                                "}"
                    },
                    new Models.Report()
                    {
                        VariableNames = new string[] { "[Script2].B" },
                        EventNames = new string[] { "[Clock].EndOfDay" }
                    }
                }
            };
            Apsim.ParentAllChildren(simulation);

            var storage = simulation.Children[2] as MockStorage;

            simulation.Run();

            double[] actual = storage.Get<double>("[Script2].B");
            double[] expected = new double[] { 2 };
            Assert.AreNotEqual(expected, actual);
        }
    }
}
