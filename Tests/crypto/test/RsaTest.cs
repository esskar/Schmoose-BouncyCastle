using System;

using NUnit.Framework;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Utilities.Test;

namespace Org.BouncyCastle.Crypto.Tests
{
	[TestFixture]
	public class RsaTest
		: SimpleTest
	{
		static IBigInteger  mod = new BigInteger("b259d2d6e627a768c94be36164c2d9fc79d97aab9253140e5bf17751197731d6f7540d2509e7b9ffee0a70a6e26d56e92d2edd7f85aba85600b69089f35f6bdbf3c298e05842535d9f064e6b0391cb7d306e0a2d20c4dfb4e7b49a9640bdea26c10ad69c3f05007ce2513cee44cfe01998e62b6c3637d3fc0391079b26ee36d5", 16);
		static IBigInteger  pubExp = new BigInteger("11", 16);
		static IBigInteger  privExp = new BigInteger("92e08f83cc9920746989ca5034dcb384a094fb9c5a6288fcc4304424ab8f56388f72652d8fafc65a4b9020896f2cde297080f2a540e7b7ce5af0b3446e1258d1dd7f245cf54124b4c6e17da21b90a0ebd22605e6f45c9f136d7a13eaac1c0f7487de8bd6d924972408ebb58af71e76fd7b012a8d0e165f3ae2e5077a8648e619", 16);
		static IBigInteger  p = new BigInteger("f75e80839b9b9379f1cf1128f321639757dba514642c206bbbd99f9a4846208b3e93fbbe5e0527cc59b1d4b929d9555853004c7c8b30ee6a213c3d1bb7415d03", 16);
		static IBigInteger  q = new BigInteger("b892d9ebdbfc37e397256dd8a5d3123534d1f03726284743ddc6be3a709edb696fc40c7d902ed804c6eee730eee3d5b20bf6bd8d87a296813c87d3b3cc9d7947", 16);
		static IBigInteger  pExp = new BigInteger("1d1a2d3ca8e52068b3094d501c9a842fec37f54db16e9a67070a8b3f53cc03d4257ad252a1a640eadd603724d7bf3737914b544ae332eedf4f34436cac25ceb5", 16);
		static IBigInteger  qExp = new BigInteger("6c929e4e81672fef49d9c825163fec97c4b7ba7acb26c0824638ac22605d7201c94625770984f78a56e6e25904fe7db407099cad9b14588841b94f5ab498dded", 16);
		static IBigInteger  crtCoef = new BigInteger("dae7651ee69ad1d081ec5e7188ae126f6004ff39556bde90e0b870962fa7b926d070686d8244fe5a9aa709a95686a104614834b0ada4b10f53197a5cb4c97339", 16);

		static string input = "4e6f77206973207468652074696d6520666f7220616c6c20676f6f64206d656e";

		//
		// to check that we handling byte extension by big number correctly.
		//
		static string edgeInput = "ff6f77206973207468652074696d6520666f7220616c6c20676f6f64206d656e";

		static byte[] oversizedSig = Hex.Decode("01ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff004e6f77206973207468652074696d6520666f7220616c6c20676f6f64206d656e");
		static byte[] dudBlock = Hex.Decode("000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff004e6f77206973207468652074696d6520666f7220616c6c20676f6f64206d656e");
		static byte[] truncatedDataBlock = Hex.Decode("0001ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff004e6f77206973207468652074696d6520666f7220616c6c20676f6f64206d656e");
		static byte[] incorrectPadding = Hex.Decode("0001ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff4e6f77206973207468652074696d6520666f7220616c6c20676f6f64206d656e");
		static byte[] missingDataBlock = Hex.Decode("0001ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff");

		public override string Name
		{
			get { return "RSA"; }
		}

		private void doTestStrictPkcs1Length(RsaKeyParameters pubParameters, RsaKeyParameters privParameters)
		{
			IAsymmetricBlockCipher   eng = new RsaEngine();

			eng.Init(true, privParameters);

			byte[] data = null;

			try
			{
				data = eng.ProcessBlock(oversizedSig, 0, oversizedSig.Length);
			}
			catch (Exception e)
			{
				Fail("RSA: failed - exception " + e.ToString(), e);
			}

			eng = new Pkcs1Encoding(eng);

			eng.Init(false, pubParameters);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);

				Fail("oversized signature block not recognised");
			}
			catch (InvalidCipherTextException e)
			{
				if (!e.Message.Equals("block incorrect size"))
				{
					Fail("RSA: failed - exception " + e.ToString(), e);
				}
			}


			// Create the encoding with StrictLengthEnabled=false (done thru environment in Java version)
			Pkcs1Encoding.StrictLengthEnabled = false;

			eng = new Pkcs1Encoding(new RsaEngine());

			eng.Init(false, pubParameters);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (InvalidCipherTextException e)
			{
				Fail("RSA: failed - exception " + e.ToString(), e);
			}

			Pkcs1Encoding.StrictLengthEnabled = true;
		}

		private void doTestTruncatedPkcs1Block(RsaKeyParameters pubParameters, RsaKeyParameters privParameters)
		{
			checkForPkcs1Exception(pubParameters, privParameters, truncatedDataBlock, "block truncated");
		}

		private void doTestDudPkcs1Block(RsaKeyParameters pubParameters, RsaKeyParameters privParameters)
		{
			checkForPkcs1Exception(pubParameters, privParameters, dudBlock, "unknown block type");
		}

		private void doTestWrongPaddingPkcs1Block(RsaKeyParameters pubParameters, RsaKeyParameters privParameters)
		{
			checkForPkcs1Exception(pubParameters, privParameters, incorrectPadding, "block padding incorrect");
		}

		private void doTestMissingDataPkcs1Block(RsaKeyParameters pubParameters, RsaKeyParameters privParameters)
		{
			checkForPkcs1Exception(pubParameters, privParameters, missingDataBlock, "no data in block");
		}

		private void checkForPkcs1Exception(RsaKeyParameters pubParameters, RsaKeyParameters privParameters, byte[] inputData, string expectedMessage)
		{
			IAsymmetricBlockCipher   eng = new RsaEngine();

			eng.Init(true, privParameters);

			byte[] data = null;

			try
			{
				data = eng.ProcessBlock(inputData, 0, inputData.Length);
			}
			catch (Exception e)
			{
				Fail("RSA: failed - exception " + e.ToString(), e);
			}

			eng = new Pkcs1Encoding(eng);

			eng.Init(false, pubParameters);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);

				Fail("missing data block not recognised");
			}
			catch (InvalidCipherTextException e)
			{
				if (!e.Message.Equals(expectedMessage))
				{
					Fail("RSA: failed - exception " + e.ToString(), e);
				}
			}
		}

		private void doTestOaep(RsaKeyParameters pubParameters, RsaKeyParameters privParameters)
		{
			//
			// OAEP - public encrypt, private decrypt
			//
			IAsymmetricBlockCipher eng = new OaepEncoding(new RsaEngine());
			byte[] data = Hex.Decode(input);

			eng.Init(true, pubParameters);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString(), e);
			}

			eng.Init(false, privParameters);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString(), e);
			}

			if (!input.Equals(Hex.ToHexString(data)))
			{
				Fail("failed OAEP Test");
			}
		}

		// TODO Move this when other JCE tests are ported from Java
		/**
		 * signature with a "forged signature" (sig block not at end of plain text)
		 */
		private void doTestBadSig()//PrivateKey priv, PublicKey pub)
		{
//			Signature           sig = Signature.getInstance("SHA1WithRSAEncryption", "BC");
			ISigner sig = SignerUtilities.GetSigner("SHA1WithRSAEncryption");
//			KeyPairGenerator    fact;
//			KeyPair             keyPair;
//			byte[]              data = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };

//			fact = KeyPairGenerator.getInstance("RSA", "BC");
			RsaKeyPairGenerator fact = new RsaKeyPairGenerator();

//			fact.initialize(768, new SecureRandom());
			RsaKeyGenerationParameters factParams = new RsaKeyGenerationParameters(
//				BigInteger.ValueOf(0x11), new SecureRandom(), 768, 25);
				BigInteger.ValueOf(3), new SecureRandom(), 768, 25);
			fact.Init(factParams);

//			keyPair = fact.generateKeyPair();
//
//			PrivateKey  signingKey = keyPair.getPrivate();
//			PublicKey   verifyKey = keyPair.getPublic();
			IAsymmetricCipherKeyPair keyPair = fact.GenerateKeyPair();

			IAsymmetricKeyParameter priv = keyPair.Private;
			IAsymmetricKeyParameter pub = keyPair.Public;

//			testBadSig(signingKey, verifyKey);





//			MessageDigest sha1 = MessageDigest.getInstance("SHA1", "BC");
			IDigest sha1 = DigestUtilities.GetDigest("SHA1");

//			Cipher signer = Cipher.getInstance("RSA/ECB/PKCS1Padding", "BC");
//			IBufferedCipher signer = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");
			IAsymmetricBlockCipher signer = new Pkcs1Encoding(new RsaEngine());

//			signer.init(Cipher.ENCRYPT_MODE, priv);
			signer.Init(true, priv);

//			byte[] block = new byte[signer.getBlockSize()];
//			byte[] block = new byte[signer.GetBlockSize()];
			byte[] block = new byte[signer.GetInputBlockSize()];

//			sha1.update((byte)0);
			sha1.Update(0);

//			byte[] sigHeader = Hex.decode("3021300906052b0e03021a05000414");
			byte[] sigHeader = Hex.Decode("3021300906052b0e03021a05000414");
//			System.arraycopy(sigHeader, 0, block, 0, sigHeader.length);
			Array.Copy(sigHeader, 0, block, 0, sigHeader.Length);

//			sha1.digest(block, sigHeader.length, sha1.getDigestLength());
			sha1.DoFinal(block, sigHeader.Length);

//			System.arraycopy(sigHeader, 0, block,
//				sigHeader.length + sha1.getDigestLength(), sigHeader.length);
			Array.Copy(sigHeader, 0, block,
				sigHeader.Length + sha1.GetDigestSize(), sigHeader.Length);

//			byte[] sigBytes = signer.doFinal(block);
			byte[] sigBytes = signer.ProcessBlock(block, 0, block.Length);

//			Signature verifier = Signature.getInstance("SHA1WithRSA", "BC");
			ISigner verifier = SignerUtilities.GetSigner("SHA1WithRSA");

//			verifier.initVerify(pub);
			verifier.Init(false, pub);

//			verifier.update((byte)0);
			verifier.Update(0);

//			if (verifier.verify(sig))
			if (verifier.VerifySignature(sigBytes))
			{
//				fail("bad signature passed");
				Fail("bad signature passed");
			}
		}

		private void testZeroBlock(ICipherParameters encParameters, ICipherParameters decParameters)
		{
			IAsymmetricBlockCipher eng = new Pkcs1Encoding(new RsaEngine());
		
			eng.Init(true, encParameters);

			if (eng.GetOutputBlockSize() != ((Pkcs1Encoding)eng).GetUnderlyingCipher().GetOutputBlockSize())
			{
				Fail("PKCS1 output block size incorrect");
			}

			byte[] zero = new byte[0];
			byte[] data = null;

			try
			{
				data = eng.ProcessBlock(zero, 0, zero.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString(), e);
			}

			eng.Init(false, decParameters);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString(), e);
			}

			if (!Arrays.AreEqual(zero, data))
			{
				Fail("failed PKCS1 zero Test");
			}
		}

		public override void PerformTest()
		{
			RsaKeyParameters pubParameters = new RsaKeyParameters(false, mod, pubExp);
			RsaKeyParameters privParameters = new RsaPrivateCrtKeyParameters(mod, pubExp, privExp, p, q, pExp, qExp, crtCoef);
			byte[] data = Hex.Decode(edgeInput);

			//
			// RAW
			//
			IAsymmetricBlockCipher   eng = new RsaEngine();

			eng.Init(true, pubParameters);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("RSA: failed - exception " + e.ToString());
			}

			eng.Init(false, privParameters);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString());
			}

			if (!edgeInput.Equals(Hex.ToHexString(data)))
			{
				Fail("failed RAW edge Test");
			}

			data = Hex.Decode(input);

			eng.Init(true, pubParameters);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString());
			}

			eng.Init(false, privParameters);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString());
			}

			if (!input.Equals(Hex.ToHexString(data)))
			{
				Fail("failed RAW Test");
			}

			//
			// PKCS1 - public encrypt, private decrypt
			//
			eng = new Pkcs1Encoding(eng);

			eng.Init(true, pubParameters);

			if (eng.GetOutputBlockSize() != ((Pkcs1Encoding)eng).GetUnderlyingCipher().GetOutputBlockSize())
			{
				Fail("PKCS1 output block size incorrect");
			}

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString());
			}

			eng.Init(false, privParameters);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString());
			}

			if (!input.Equals(Hex.ToHexString(data)))
			{
				Fail("failed PKCS1 public/private Test");
			}

			//
			// PKCS1 - private encrypt, public decrypt
			//
			eng = new Pkcs1Encoding(((Pkcs1Encoding)eng).GetUnderlyingCipher());

			eng.Init(true, privParameters);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString());
			}

			eng.Init(false, pubParameters);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString());
			}

			if (!input.Equals(Hex.ToHexString(data)))
			{
				Fail("failed PKCS1 private/public Test");
			}

			testZeroBlock(pubParameters, privParameters);
			testZeroBlock(privParameters, pubParameters);

			//
			// key generation test
			//
			RsaKeyPairGenerator  pGen = new RsaKeyPairGenerator();
			RsaKeyGenerationParameters  genParam = new RsaKeyGenerationParameters(
				BigInteger.ValueOf(0x11), new SecureRandom(), 768, 25);

			pGen.Init(genParam);

			IAsymmetricCipherKeyPair  pair = pGen.GenerateKeyPair();

			eng = new RsaEngine();

			if (((RsaKeyParameters)pair.Public).Modulus.BitLength < 768)
			{
				Fail("failed key generation (768) length test");
			}

			eng.Init(true, pair.Public);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString());
			}

			eng.Init(false, pair.Private);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString());
			}

			if (!input.Equals(Hex.ToHexString(data)))
			{
				Fail("failed key generation (768) Test");
			}

			genParam = new RsaKeyGenerationParameters(BigInteger.ValueOf(0x11), new SecureRandom(), 1024, 25);

			pGen.Init(genParam);
			pair = pGen.GenerateKeyPair();

			eng.Init(true, pair.Public);

			if (((RsaKeyParameters)pair.Public).Modulus.BitLength < 1024)
			{
				Fail("failed key generation (1024) length test");
			}

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString());
			}

			eng.Init(false, pair.Private);

			try
			{
				data = eng.ProcessBlock(data, 0, data.Length);
			}
			catch (Exception e)
			{
				Fail("failed - exception " + e.ToString());
			}

			if (!input.Equals(Hex.ToHexString(data)))
			{
				Fail("failed key generation (1024) test");
			}

			genParam = new RsaKeyGenerationParameters(
				BigInteger.ValueOf(0x11), new SecureRandom(), 16, 25);
			pGen.Init(genParam);

			for (int i = 0; i < 100; ++i)
			{
				pair = pGen.GenerateKeyPair();
				RsaPrivateCrtKeyParameters privKey = (RsaPrivateCrtKeyParameters) pair.Private;
				IBigInteger pqDiff = privKey.P.Subtract(privKey.Q).Abs();

				if (pqDiff.BitLength < 5)
				{
					Fail("P and Q too close in RSA key pair");
				}
			}

			doTestBadSig();
			doTestOaep(pubParameters, privParameters);
			doTestStrictPkcs1Length(pubParameters, privParameters);
			doTestDudPkcs1Block(pubParameters, privParameters);
			doTestMissingDataPkcs1Block(pubParameters, privParameters);
			doTestTruncatedPkcs1Block(pubParameters, privParameters);
			doTestWrongPaddingPkcs1Block(pubParameters, privParameters);

			try
			{
				new RsaEngine().ProcessBlock(new byte[]{ 1 }, 0, 1);
				Fail("failed initialisation check");
			}
			catch (InvalidOperationException)
			{
				// expected
			}
		}

		public static void Main(
			string[] args)
		{
			ITest test = new RsaTest();
			ITestResult result = test.Perform();

			Console.WriteLine(result);
		}

		[Test]
		public void TestFunction()
		{
			string resultText = Perform().ToString();

			Assert.AreEqual(Name + ": Okay", resultText);
		}
	}
}
