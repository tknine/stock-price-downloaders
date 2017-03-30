# Price Downloaders Projects

These are samples of price downloaders for various finanacial data sources.

## Yahoo Historical Data

**YahooPriceDownloader** will download any number of specified symbols of data. This data will in a non-split adjusted format where the **Adjusted Close** is the last field of data on each record.  A future version of this application will download provide the option adjusting all the fields based on the **Adjusted Close**.

```
YahooPriceDownloader.exe

  -s, --symbols      (Default: GOOG) Input symbols to download. (Quoted and space separated if more than one symbol)
  -f, --file         Input symbol file list to download.  (One symbol per line)
  -d, --dir          (Default: Data) Folder to put donwloads into. (Will be created if does not exist.)
  -e, --enddate      End date of data download. (Default current date)
  -b, --begindate    End date of data download. (Default 1 year before end date)
  -r, --sort         (Default: true) Sort data by date.
  -t, --threads      (Default: 5) Number of threads for downloading.
  --help             Display this help screen.
  --version          Display version information.
```