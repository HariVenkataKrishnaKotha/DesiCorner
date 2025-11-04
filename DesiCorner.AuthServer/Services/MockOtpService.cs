/*using DesiCorner.AuthServer.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Intrinsics.X86;
using Twilio.TwiML.Voice;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Task = System.Threading.Tasks.Task;

namespace DesiCorner.AuthServer.Services;

public class MockOtpService : IOtpService
{
    private readonly ILogger<MockOtpService> _logger;

    public MockOtpService(ILogger<MockOtpService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendOtpAsync(string phoneNumber, string purpose, CancellationToken ct = default)
    {
        _logger.LogWarning("MOCK OTP for {Phone}: 123456 (Purpose: {Purpose})", phoneNumber, purpose);
        return Task.FromResult(true);
    }

    public Task<(bool isValid, string? error)> ValidateOtpAsync(string phoneNumber, string otp, CancellationToken ct = default)
    {
        // Accept any OTP for testing
        var isValid = otp == "123456";
        return Task.FromResult(isValid ? (true, (string?)null) : (false, "Invalid OTP - use 123456 for testing"));
    }

    public Task<int> GetRemainingAttemptsAsync(string phoneNumber, CancellationToken ct = default)
    {
        return Task.FromResult(3);
    }
}*/