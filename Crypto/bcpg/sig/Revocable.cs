using System;



namespace Org.BouncyCastle.Bcpg.Sig
{
    /**
    * packet giving whether or not is revocable.
    */
    public class Revocable : SignatureSubpacket
    {
        private static byte[] BooleanToByteArray(
            bool value)
        {
            var data = new byte[1];

            if (value)
            {
                data[0] = 1;
                return data;
            }
            return data;
        }

        public Revocable(bool critical, byte[] data)
            : base(SignatureSubpacketTag.Revocable, critical, data)
        {
        }

        public Revocable(bool critical, bool isRevocable)
            : base(SignatureSubpacketTag.Revocable, critical, BooleanToByteArray(isRevocable))
        {
        }

        public bool IsRevocable()
        {
            return Data[0] != 0;
        }
    }
}
