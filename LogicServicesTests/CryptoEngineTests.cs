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
		private void VerifyGenerateCert()
		{
			if (!File.Exists(Config<string>.GetInstance()["Certificate"]))
			{
				var pbMasterKeyPair = CertGen.Program.GenerateKeyPair();
				var pbKeyPair = CertGen.Program.GenerateKeyPair();

				var pbCertFile = new PBCertFile();

				/* Copy public master key */
				pbCertFile.MasterPublic = new PBPublicKey();
				pbCertFile.MasterPublic.PublicKey = pbMasterKeyPair.PublicKey.PublicKey;
				pbCertFile.Cert = CertGen.Program.SignCertificate(Config<string>.GetInstance()["Certificate"], pbMasterKeyPair, pbKeyPair);
				pbCertFile.Keys = pbKeyPair;

				using (var fs = File.Open(Config<string>.GetInstance()["Certificate"], FileMode.Create))
				{
					pbCertFile.WriteTo(fs);
				}
			}
		}

		[TestInitialize()]
		public void InitializeCrypto()
		{
			if (!File.Exists("DBDump.sqlite"))
			{
				File.Copy("..\\..\\DBDump.sqlite", "DBDump.sqlite");
			}

			if (!File.Exists("QuickStream.ini"))
			{
				File.Copy("..\\..\\QuickStream.ini", "QuickStream.ini");
			}

			VerifyGenerateCert();

			new CryptoEngine().loadCertificate("qs1.cert");
		}

		[TestMethod()]
		public void ECLoadTest()
		{
			var ec = new CryptoEngine().ECLoad(CryptoEngine.Certificate.Keys.PublicKey.PublicKey.ToByteArray(), CryptoEngine.Certificate.Keys.PrivateKey.ToByteArray());

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
			var ec = new CryptoEngine().ECLoad(CryptoEngine.Certificate.Keys.PublicKey.PublicKey.ToByteArray(), CryptoEngine.Certificate.Keys.PrivateKey.ToByteArray());

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
			var ec = new CryptoEngine().ECLoad(CryptoEngine.Certificate.Keys.PublicKey.PublicKey.ToByteArray(), CryptoEngine.Certificate.Keys.PrivateKey.ToByteArray());

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