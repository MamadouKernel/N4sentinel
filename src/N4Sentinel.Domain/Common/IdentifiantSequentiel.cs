using System.Security.Cryptography;

namespace N4Sentinel.Domain.Common;

/// <summary>
/// Génère des identifiants UUID v7 : 48 bits d'horodatage Unix en millisecondes, puis de
/// l'aléatoire. Le préfixe horodaté rend les clés croissantes dans le temps, ce qui évite la
/// fragmentation des index en base — un GUID v4 insère au hasard dans l'arbre.
///
/// .NET 9 fournit <c>Guid.CreateVersion7()</c>. Le projet cible .NET 8, où il n'existe pas :
/// la génération est donc écrite ici, conforme à la RFC 9562. À remplacer par l'appel du
/// framework le jour où la solution remontera de version.
/// </summary>
public static class IdentifiantSequentiel
{
    public static Guid Nouveau()
    {
        Span<byte> octets = stackalloc byte[16];

        // 48 bits d'horodatage, gros-boutiste, comme l'impose la RFC.
        var horodatage = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        octets[0] = (byte)(horodatage >> 40);
        octets[1] = (byte)(horodatage >> 32);
        octets[2] = (byte)(horodatage >> 24);
        octets[3] = (byte)(horodatage >> 16);
        octets[4] = (byte)(horodatage >> 8);
        octets[5] = (byte)horodatage;

        RandomNumberGenerator.Fill(octets[6..]);

        // Version 7 sur les quatre bits hauts de l'octet 6.
        octets[6] = (byte)((octets[6] & 0x0F) | 0x70);

        // Variante RFC 4122 sur les deux bits hauts de l'octet 8.
        octets[8] = (byte)((octets[8] & 0x3F) | 0x80);

        return new Guid(octets, bigEndian: true);
    }
}
