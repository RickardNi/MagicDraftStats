# MagicDraftStats

An application for analyzing _Magic: The Gathering_ draft statistics and gameplay data, importing data from [BGStats](https://www.bgstatsapp.com/). This is a Blazor WebAssembly (WASM) application built with .NET 10.

I mainly use it to track performance of our drafts, which is mainly the [Magic Foundations cube](https://cubecobra.com/cube/list/5b73c9bb-4928-4d6a-9580-30d5a718d925).

The project is based on my previous project [MagicDeckStats](https://github.com/RickardNi/MagicDeckStats).

**🌐 [Demo](https://rickardni.github.io/MagicDraftStats/)**

## FAQ

### **How do I import data to this app?**
You need to export data first from BGStats:

1. Open BGStats on your device
2. Go to **"Settings"**
3. Select **"Export, import and backup"**
4. Select **"Export backup file..."**
5. Choose a location to save the file

This will give you a file called `BGStatsExport.json` that you can upload to the site on the **"Import / Export"** page.

### **Is my data stored on a server?**
No, this is a client-side application. All data processing happens in your browser, and no data is sent to external servers. Your data remains private and local to your device.
