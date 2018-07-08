using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using argparse;
using Google.Protobuf;
using LogicServices;
using NDesk.Options;

namespace CertGen
{
	internal class Program
	{
		private const string KEYNAME = "KeyPair";

		private const int HEADER_SIZE = 8;
		private const int PUBLIC_KEY_SIZE = 64;
		private const int PRIVATE_KEY_SIZE = 32;

		private static PBKeyPair GenerateKeyPair()
		{
			var pbKeyPair = new PBKeyPair();

			if (CngKey.Exists(KEYNAME))
			{
				CngKey.Open(KEYNAME).Delete();
			}

			var keyPair = CngKey.Create(CngAlgorithm.ECDsaP256, KEYNAME, new CngKeyCreationParameters
			{
				ExportPolicy = CngExportPolicies.AllowPlaintextExport,
				KeyUsage = CngKeyUsages.AllUsages,
			});

			var pub = keyPair.Export(CngKeyBlobFormat.GenericPublicBlob);
			var prv = keyPair.Export(CngKeyBlobFormat.GenericPrivateBlob);

			pbKeyPair.PublicKey =
				new PBPublicKey {PublicKey = ByteString.CopyFrom(pub, HEADER_SIZE, pub.Length - HEADER_SIZE)};
			pbKeyPair.PrivateKey = ByteString.CopyFrom(prv, HEADER_SIZE + PUBLIC_KEY_SIZE, prv.Length - HEADER_SIZE - PUBLIC_KEY_SIZE);

			return pbKeyPair;
		}

		private static PBCertificate SignCertificate(string name, PBKeyPair masterKeyPair, PBKeyPair keyPair)
		{
			var dsa = CryptoEngine.GetInstance().ECLoad(masterKeyPair.PublicKey.PublicKey, masterKeyPair.PrivateKey);

			var cert = new PBCertificate
			{
				Name = name,
				PublicKey = keyPair.PublicKey.PublicKey
			};

			var byteCert = dsa.SignData(keyPair.PublicKey.PublicKey.ToByteArray(), HashAlgorithmName.SHA256);
			cert.Signature = ByteString.CopyFrom(byteCert, 0, byteCert.Length);

			return cert;
		}

		private const string GEN = "gen";
		private const string CERT = "cert";
		
		private static void Main(string[] args)
		{
			var options = new Dictionary<string, string>();
			var opt = new OptionSet
			{
				{
					"o|output=", "Output filename",
					name => options["name"] = name
				},
				{
					"t|type=", "<" + GEN + "|" + CERT + "> Generate a new key pair or generate a certificate",
					type => options["type"] = type
				},
				{
					"m|master=", "Master key file used to generate certificate",
					m => options["master"] = m
				}
			};

			opt.Parse(args);

			if (!options.ContainsKey("type") || !options.ContainsKey("name") || (options["type"] == CERT && !options.ContainsKey("master")))
			{
				Console.WriteLine("CertGen - Certificate Generator");
				opt.WriteOptionDescriptions(Console.Out);

				return;
			}

			var pbKeyPair = GenerateKeyPair();

			using (var fs = File.Create(options["name"] + ".key"))
			{
				pbKeyPair.WriteTo(fs);

				fs.Close();
			}

			if (options["type"] == CERT)
			{
				var pbMasterKeyPair = new PBKeyPair();

				using (var fs = File.Open(options["master"] + ".key", FileMode.Open))
				{
					pbMasterKeyPair.MergeFrom(fs);
				}

				using (var fs = File.Create(options["master"] + ".key.pub"))
				{
					pbMasterKeyPair.PublicKey.WriteTo(fs);

					fs.Close();
				}

				var pbCert = SignCertificate(options["name"], pbMasterKeyPair, pbKeyPair);

				using (var fs = File.Open(options["name"] + ".cert", FileMode.Create))
				{
					pbCert.WriteTo(fs);
				}
			}

			Console.WriteLine("Done");
		}
	}
}