using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using JSON;
using JSON.PartnerRequests;

namespace LogicServices
{
	public class CryptoEngine : Singleton<CryptoEngine>
	{
		public PBCertFile Certificate { get; set; }
		private bool initialized = false;

		private const int HEADER_SIZE = 8;
		private const int PUBLIC_KEY_SIZE = 64;
		private const int PRIVATE_KEY_SIZE = 32;

		public ECDsaCng ECLoad(byte[] pubkey, byte[] prvkey)
		{
			var encodedPrivateKey = new byte[HEADER_SIZE + PUBLIC_KEY_SIZE + PRIVATE_KEY_SIZE];
			encodedPrivateKey[0] = 0x45;
			encodedPrivateKey[1] = 0x43;
			encodedPrivateKey[2] = 0x53;
			encodedPrivateKey[3] = 0x32;
			encodedPrivateKey[4] = 0x20;
			encodedPrivateKey[5] = 0x00;
			encodedPrivateKey[6] = 0x00;
			encodedPrivateKey[7] = 0x00;

			pubkey.CopyTo(encodedPrivateKey, HEADER_SIZE);
			prvkey.CopyTo(encodedPrivateKey, HEADER_SIZE + PUBLIC_KEY_SIZE);

			return new ECDsaCng(CngKey.Import(encodedPrivateKey, CngKeyBlobFormat.GenericPrivateBlob));
		}

		public ECDsaCng ECLoad(ByteString pubkey, ByteString prvkey)
		{
			return this.ECLoad(pubkey.ToByteArray(), prvkey.ToByteArray());
		}

		public ECDsaCng ECLoad(byte[] pubkey)
		{
			var encodedPublicKey = new byte[HEADER_SIZE + PUBLIC_KEY_SIZE];
			encodedPublicKey[0] = 0x45;
			encodedPublicKey[1] = 0x43;
			encodedPublicKey[2] = 0x53;
			encodedPublicKey[3] = 0x31;
			encodedPublicKey[4] = 0x20;
			encodedPublicKey[5] = 0x00;
			encodedPublicKey[6] = 0x00;
			encodedPublicKey[7] = 0x00;

			pubkey.CopyTo(encodedPublicKey, HEADER_SIZE);

			return new ECDsaCng(CngKey.Import(encodedPublicKey, CngKeyBlobFormat.GenericPublicBlob));
		}

		public ECDsaCng ECLoad(ByteString pubkey)
		{
			return this.ECLoad(pubkey.ToByteArray());
		}

		public void loadCertificate(string certfile)
		{
			if (! this.initialized)
			{
				using (var fs = File.Open(certfile, FileMode.Open))
				{
					this.Certificate = new PBCertFile();
					this.Certificate.MergeFrom(fs);
				}

				this.initialized = true;
			}
		}

		public ECDsaCng verifyCertificate(byte[] key, int certId, byte[] signature)
		{
			byte[] signBuff = new byte[key.Length + sizeof(int)];
			key.CopyTo(signBuff, 0);
			for (int i = 0; i < sizeof(int); ++i)
			{
				signBuff[key.Length + i] = (byte)((certId >> (8 * i)) & 0xff);
			}

			var dsa = this.ECLoad(this.Certificate.MasterPublic.PublicKey);

			if (!dsa.VerifyData(signBuff, signature, HashAlgorithmName.SHA256))
			{
				throw new CryptographicException("Certificate verification failed");
			}

			return this.ECLoad(key);
		}

		public ECDsaCng verifyCertificate(PBCertificate cert)
		{
			return this.verifyCertificate(cert.PublicKey.ToByteArray(), cert.Id, cert.Signature.ToByteArray());
		}
	}
}
