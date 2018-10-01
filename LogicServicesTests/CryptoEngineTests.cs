using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogicServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Google.Protobuf;

namespace LogicServices.Tests
{
	[TestClass()]
	public class CryptoEngineTests
	{
		[TestMethod()]
		public void ECLoadTest()
		{
			new CryptoEngine().loadCertificate("qs0.cert");

			var ec = new CryptoEngine().ECLoad(CryptoEngine.Certificate.MasterPublic.ToByteArray());

			var data = Encoding.ASCII.GetBytes("Hello, world!");

			var sig = ec.SignData(data);

			sig[0]++;

			if (ec.VerifyData(data, sig))
			{
				Assert.Fail("Signature verified despite corruption");
			}
		}

		[TestMethod()]
		public void verifyCertificateTestSimpleVerifyCert()
		{
			new CryptoEngine().loadCertificate("qs0.cert");

			try
			{
				new CryptoEngine().verifyCertificate(CryptoEngine.Certificate.Cert);
			}
			catch (Exception e)
			{
				Assert.Fail(e.Message);
			}
		}

		[TestMethod()]
		public void verifyCertificateTestNegativeVerifyCert()
		{
			new CryptoEngine().loadCertificate("qs0.cert");

			var pub = CryptoEngine.Certificate.Cert.PublicKey.ToByteArray();
			var id = CryptoEngine.Certificate.Cert.Id;
			var cert = CryptoEngine.Certificate.Cert.Signature.ToByteArray();

			cert[0]++;

			try
			{
				new CryptoEngine().verifyCertificate(CryptoEngine.Certificate.Cert);
				Assert.Fail("Certificate verified despite being incorrect.");
			}
			catch
			{
				/* Do nothing. This is a negative test. */
			}
		}

		[TestMethod()]
		public void verifyCertificateTestVerifyData()
		{
			new CryptoEngine().loadCertificate("qs0.cert");

			var ec = new CryptoEngine().ECLoad(CryptoEngine.Certificate.MasterPublic.ToByteArray());

			var data = Encoding.ASCII.GetBytes("Hello, world!");

			var sig = ec.SignData(data);

			if (!new CryptoEngine().verifyCertificate(CryptoEngine.Certificate.Cert).VerifyData(data, sig))
			{
				Assert.Fail("Failed verifying data");
			}
		}

		[TestMethod()]
		public void verifyCertificateTestNegativeVerifyData()
		{
			new CryptoEngine().loadCertificate("qs0.cert");

			var ec = new CryptoEngine().ECLoad(CryptoEngine.Certificate.MasterPublic.ToByteArray());

			var data = Encoding.ASCII.GetBytes("Hello, world!");

			var sig = ec.SignData(data);

			sig[0]++;

			if (new CryptoEngine().verifyCertificate(CryptoEngine.Certificate.Cert).VerifyData(data, sig))
			{
				Assert.Fail("Signature verified despite corruption");
			}
		}
	}
}