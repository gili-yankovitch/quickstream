using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;

namespace LogicServices
{
	public class CryptoEngine : Singleton<CryptoEngine>
	{
		private PBPublicKey masterKey = null;

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

		private void lazyLoadCert()
		{
			/* Lazy-load master key */
			if (masterKey == null)
			{
				using (var fs = File.Open("master.key.pub", FileMode.Open))
				{
					this.masterKey = new PBPublicKey();
					this.masterKey.MergeFrom(fs);
				}
			}
		}

		public ECDsaCng verifyCertificate(byte[] key, byte[] signature)
		{
			this.lazyLoadCert();

			var dsa = this.ECLoad(this.masterKey.PublicKey);

			if (!dsa.VerifyData(key, signature, HashAlgorithmName.SHA256))
			{
				throw new CryptographicException("Certificate verification failed");
			}

			return this.ECLoad(key);
		}

		public ECDsaCng verifyCertificate(PBCertificate cert)
		{
			return this.verifyCertificate(cert.PublicKey.ToByteArray(), cert.Signature.ToByteArray());
		}
	}
}
