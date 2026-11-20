using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Crops;

public sealed class UpdateCropStatusRequest
{
    public bool IsActive { get; set; }
}