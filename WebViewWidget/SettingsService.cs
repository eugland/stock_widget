using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OoplesFinance.YahooFinanceAPI.Models;

namespace WebViewWidget
{
    /// <summary>
    /// A singleton service to manage application settings, persisting them to a local JSON file.
    /// </summary>
    public sealed class SettingsService
    {
        // --- Singleton Implementation ---
        // This ensures only one instance of SettingsService ever exists.
        private static readonly Lazy<SettingsService> _instance = new Lazy<SettingsService>(() => new SettingsService());
        public static SettingsService Instance => _instance.Value;
        // --- End Singleton ---

        private readonly string _filePath;

        /// <summary>
        /// The list of stock symbols in the user's portfolio.
        /// </summary>
        public List<AutoCompleteResult> PortfolioSymbols { get; private set; }

        /// <summary>
        /// Private constructor to prevent creating new instances and to load settings on startup.
        /// </summary>
        private SettingsService()
        {
            // Define the path where the settings file will be stored.
            // This is typically in C:\Users\<YourUser>\AppData\Roaming\WebViewWidget\settings.json
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolderPath = Path.Combine(appDataPath, "WebViewWidget");
            _filePath = Path.Combine(appFolderPath, "settings.json");

            PortfolioSymbols = new List<AutoCompleteResult>();
            LoadSettings();
        }

        /// <summary>
        /// Loads the settings from the JSON file into memory.
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    var savedSymbols = JsonSerializer.Deserialize<List<AutoCompleteResult>>(json);
                    if (savedSymbols != null)
                    {
                        PortfolioSymbols = savedSymbols;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle potential errors like corrupted files
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                PortfolioSymbols = []; // Reset to default if loading fails
            }
        }

        /// <summary>
        /// Saves the current settings from memory to the JSON file.
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

                string json = JsonSerializer.Serialize(PortfolioSymbols, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a stock symbol to the portfolio and saves the changes.
        /// </summary>
        public void AddStock(AutoCompleteResult symbol)
        {
            if (!PortfolioSymbols.Contains(symbol))
            {
                PortfolioSymbols.Add(symbol);
                SaveSettings();
            }
        }

        /// <summary>
        /// Removes a stock symbol from the portfolio and saves the changes.
        /// </summary>
        public void RemoveStock(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return;

            // Remove all entries where the Symbol matches (case-insensitive)
            PortfolioSymbols.RemoveAll(s =>
                string.Equals(s.Symbol, symbol, StringComparison.OrdinalIgnoreCase));

            SaveSettings();
        }
    }
}
