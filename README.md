# Eric's Super Duper Secure Auth™ 🔐

> *Because who needs Google Authenticator when you can roll your own sketchy system tray app?*

## What is this monstrosity?

Welcome to the most "enterprise-grade" 2FA authenticator you've ever seen! This beautiful piece of software engineering is a Windows system tray application that generates TOTP codes. It's like Google Authenticator, but with 100% more hardcoded values and 0% architectural patterns!

## Features That Will Make You Question Everything

- ✨ **Single-file architecture** - Why use multiple files when you can cram 818 lines into one massive Program.cs?
- 🎨 **"Modern" UI** - Rounded corners! Subtle shadows! It's basically the next macOS!
- 📱 **Actually Responsive Design** - Uses em units and screen percentages like a real web developer!
- 🔒 **Security First** - All your secrets are safely embedded as resources (what could go wrong?)
- 🚀 **Blazing Fast** - Updates every 500ms whether you need it or not!
- 💾 **Memory Efficient** - Calls `GC.Collect()` manually because we're performance experts
- 🎯 **Production Ready** - Contains helpful comments like `// Better vertical centering`

## Installation

1. Clone this repository
2. Create an `accounts.json` file (see `accounts.json.example`)
3. Build with Visual Studio or `dotnet build`
4. Run and pray to the demo gods
5. Right-click the tray icon to exit (left-click for the magic!)

## Configuration

Create an `accounts.json` file with your TOTP secrets:

```json
{
  "accounts": [
    {
      "name": "Your Bank Account (totally safe)",
      "secret": "DEFINITELY_NOT_YOUR_REAL_SECRET",
      "digits": 6,
      "algorithm": "SHA1"
    }
  ]
}
```

*Note: This file is in .gitignore because we learned about security the hard way.*

## Architecture Highlights

### Design Patterns Used:
- ❌ MVC
- ❌ MVVM  
- ❌ Repository Pattern
- ❌ Dependency Injection
- ✅ **The "Everything in Main" Pattern** (Patent Pending)

### Code Quality Features:
- **CSS-like Units**: `Em(4.2f)` and `ScreenWidth(22)` - we're basically web developers now!
- **Inline Event Handlers**: Because separating concerns is overrated
- **Custom Graphics Extensions**: Why use existing UI frameworks when you can draw rounded rectangles manually?
- **Global State**: Static methods everywhere, just like the good old days!
- **DPI Awareness**: `Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)` - look how fancy we are!

## Known "Features"

- 🎉 ~~Hardcoded window positioning~~ **FIXED!** Now uses screen percentages like a pro!
- 🎉 ~~Blurry text~~ **FIXED!** Crystal clear DPI-aware rendering!
- 🎉 ~~Terrible scaling~~ **FIXED!** Responsive em-based layout system!
- 🐛 No error handling for malformed JSON (crashes are features!)
- 🐛 Memory leaks? What memory leaks? We call `GC.Collect()`!
- 🐛 Thread safety is for enterprise applications
- 🐛 No unit tests (testing is for people who don't believe in their code)

## FAQ

**Q: Why does this exist?**  
A: I was tired of using my phone.

**Q: Is this secure?**  
A: As secure as storing your passwords in a text file named "definitely_not_passwords.txt"

**Q: Can I use this in production?**  
A: You *can* do many things. Should you? That's between you and your conscience.

**Q: Why is everything in one file?**  
A: Microservices are overrated. We're bringing back the monolith, one giant Program.cs at a time.

**Q: Why does it look like macOS?**  
A: Don't flatter me, it looks like shit.

## Contributing

Found a bug? Great! Here's how you can help:

1. ~~Fork the repository~~
2. ~~Create a feature branch~~
3. ~~Write tests~~
4. Just edit Program.cs directly and add more inline code. It's the way.

## License

This code is released under the "Please Don't Use This In Production" license. 

Use at your own risk. I'm not responsible for any security breaches, mental breakdowns, or existential crises that may result from reading or using this code.

---

*"It works on my machine!" - Eric S, 2025*