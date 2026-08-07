namespace NicaRunner.Domain.Entities;

// DorsalDuplicado: dos resultados reclaman el mismo dorsal — tiene DisputeGroupId,
// dos lados. CategoriaSinSalida / CategoriaCerrada: la llegada cruzó contra una
// categoría que no tenía StartUtc o que ya estaba Terminada — conflicto contra estado,
// no contra otro resultado, así que no tiene DisputeGroupId.
public enum DisputeMotivo
{
    DorsalDuplicado,
    CategoriaSinSalida,
    CategoriaCerrada
}
