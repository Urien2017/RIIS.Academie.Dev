using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.ProcesVerbaux.Dtos;

public class GenererProcesVerbalRequestDto
{
    public long ParcoursAcademiqueId { get; set; }
    public long? SemestrePedagogiqueId { get; set; }
    public TypeProcesVerbal Type { get; set; } = TypeProcesVerbal.SessionNormale;
    public string? CodeSession { get; set; }
    public bool EstDefinitif { get; set; }
    public string? Observation { get; set; }
}
