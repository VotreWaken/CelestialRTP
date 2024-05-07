//----------------------------------------------------------------------------
// File Name: BaseSetup.cs
// 
// Description: 
// BaseSetup is responsible for All Unit Tests Setup, Defines the behavior
// of the logger, and finalization after all tests done
//
//
// Author(s):
// Egor Waken
//
// History:
// 07 May 2024	Egor Waken       Created.
//
// License:
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//----------------------------------------------------------------------------

namespace AudioWaveOutUnitTest
{
    [SetUpFixture]
    public class BaseSetup
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            // Make Build 
            // In NLog.config File need to set Copy To Output Directory
            LogManager.Configuration = new XmlLoggingConfiguration("nlog.config");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            // Finalize
        }
    }
}
