using System;
using System.Collections.Concurrent;

namespace DrawTogether.Client.Auth;

// Simple in-memory auth client for demo/flow purposes.
// Replace with real server calls later.
public class AuthClient
{
    private readonly ConcurrentDictionary<string, (string Username, string Password, string Otp)> _store = new();

    public void SendSignupOtp(string email, string username, string password, out string otp)
    {
        otp = GenerateOtp();
        _store[email] = (username, password, otp);
        // In real app, send otp to email. For demo we return it to caller.
    }

    public bool VerifySignupOtp(string email, string code)
    {
        if (!_store.TryGetValue(email, out var rec)) return false;
        if (rec.Otp == code)
        {
            // Clear otp to mark verified
            _store[email] = (rec.Username, rec.Password, string.Empty);
            return true;
        }
        return false;
    }

    public bool Register(string email, string username, string password)
    {
        // store was already populated in SendSignupOtp; ensure no otp remains
        _store[email] = (username, password, string.Empty);
        return true;
    }

    public bool SendPasswordResetOtp(string email, out string otp)
    {
        otp = GenerateOtp();
        if (_store.TryGetValue(email, out var rec))
        {
            _store[email] = (rec.Username, rec.Password, otp);
            return true;
        }
        // not found -> still generate code to keep UX consistent
        _store[email] = ("", "", otp);
        return false;
    }

    public bool VerifyPasswordResetOtp(string email, string code)
    {
        if (!_store.TryGetValue(email, out var rec)) return false;
        if (rec.Otp == code)
        {
            _store[email] = (rec.Username, rec.Password, string.Empty);
            return true;
        }
        return false;
    }

    public bool SetNewPassword(string email, string newPassword)
    {
        if (!_store.TryGetValue(email, out var rec)) return false;
        _store[email] = (rec.Username, newPassword, string.Empty);
        return true;
    }

    public bool Login(string email, string password, out string username)
    {
        username = string.Empty;
        if (!_store.TryGetValue(email, out var rec)) return false;
        if (rec.Password == password)
        {
            username = rec.Username;
            return true;
        }
        return false;
    }

    private static string GenerateOtp()
    {
        var rnd = new Random();
        return rnd.Next(0, 999999).ToString("D6");
    }
}
