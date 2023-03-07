using System;
using NeosCCF;
using NUnit.Framework;

namespace NeosCCFTest
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void TargetParsingTest()
        {
            CallTargetManager ctm = new CallTargetManager();
            
            ctm.ParseTargetString("NeosCCF.Test", out string targetName, out string[] args);
            
            Assert.True(true);
        }
    }
}