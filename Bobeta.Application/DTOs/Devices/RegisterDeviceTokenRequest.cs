using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Devices;

public record RegisterDeviceTokenRequest(string Token, DevicePlatform Platform);
