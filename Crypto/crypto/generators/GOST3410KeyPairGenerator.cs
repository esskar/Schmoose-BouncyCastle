using System;

using Org.BouncyCastle.Asn1.CryptoPro;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Org.BouncyCastle.Crypto.Generators
{
	/**
	 * a GOST3410 key pair generator.
	 * This generates GOST3410 keys in line with the method described
	 * in GOST R 34.10-94.
	 */
	public class Gost3410KeyPairGenerator
		: IAsymmetricCipherKeyPairGenerator
	{
		private Gost3410KeyGenerationParameters param;

		public void Init(
			IKeyGenerationParameters parameters)
		{
			if (parameters is Gost3410KeyGenerationParameters)
			{
				this.param = (Gost3410KeyGenerationParameters) parameters;
			}
			else
			{
				Gost3410KeyGenerationParameters kgp = new Gost3410KeyGenerationParameters(
					parameters.Random,
					CryptoProObjectIdentifiers.GostR3410x94CryptoProA);

				if (parameters.Strength != kgp.Parameters.P.BitLength - 1)
				{
					// TODO Should we complain?
				}

				this.param = kgp;
			}
		}

		public IAsymmetricCipherKeyPair GenerateKeyPair()
		{
			ISecureRandom random = param.Random;
			Gost3410Parameters gost3410Params = param.Parameters;

			IBigInteger q = gost3410Params.Q;
			IBigInteger x;
			do
			{
				x = new BigInteger(256, random);
			}
			while (x.SignValue < 1 || x.CompareTo(q) >= 0);

			IBigInteger p = gost3410Params.P;
			IBigInteger a = gost3410Params.A;

			// calculate the public key.
			IBigInteger y = a.ModPow(x, p);

			if (param.PublicKeyParamSet != null)
			{
				return new AsymmetricCipherKeyPair(
					new Gost3410PublicKeyParameters(y, param.PublicKeyParamSet),
					new Gost3410PrivateKeyParameters(x, param.PublicKeyParamSet));
			}

			return new AsymmetricCipherKeyPair(
				new Gost3410PublicKeyParameters(y, gost3410Params),
				new Gost3410PrivateKeyParameters(x, gost3410Params));
		}
	}
}
