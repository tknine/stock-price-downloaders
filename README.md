# Equity Historical Data Download Projects

These are samples of price downloaders for various finanacial data sources.

## Yahoo Historical Data

**YahooPriceDownloader** will download any number of specified symbols of data. This data will in a non-split adjusted format where the **Adjusted Close** is the last field of data on each record.  using "-a" option will use the Adjuusted Close value to adjust all the price values in each record, but the Adjusted Close that comes with the CSV download is different than the adjusted close that appears in the web site version of the data.  Given this issue, the data in the CSV download will be off once the Adjusted Cloe is applied to the unadjusted prices.  The unadjusted prices do give an accurate way to test against the real prices of the time, but make it difficult to back test against as the splits and dividends will cause sudden price changes in the data.

#### Index Symbols
These are usually downloadable through the API.

#### Volume Back Adjusting
Volume values are never back adjusted on the data services.  It is possible to do so, but it could cause the creation of volume values that exceed the maximum value of an **int**, and so the volume would have to be represented as a floating point number.

#### Future Features:
* Download splits
* Download dividends
* Auto apply splits and dividends to the unadjusted data to get prices that match google finanace and yahoo's own web interface for historical data.
* Allow for updating of files and removing duplicates.

```
YahooPriceDownloader.exe --help

  -s, --symbols      (Default: GOOG) Input symbols to download. (Quoted and space separated if more than one symbol)
  -f, --file         Input symbol file list to download.  (One symbol per line)
  -d, --dir          (Default: Data) Folder to put donwloads into. (Will be created if does not exist.)
  -e, --enddate      End date of data download. (Default current date)
  -b, --begindate    End date of data download. (Default 1 year before end date)
  -r, --sort         (Default: true) Sort data by date.
  -t, --threads      (Default: 5) Number of threads for downloading.
  -a, --adjust       (Default: false) Back adjust all data based on Adjusted Close.
  --help             Display this help screen.
  --version          Display version information.
```


## Google Historical Data

The google data is a good source for US price back adjusted data.  The only catch is that google implements a request limit based on time.  This limit equates to about 1 request per second, so any data downloading on the finance API requires following this rule.  Read about the limits here: [Google API](https://developers.google.com/analytics/devguides/reporting/core/v3/limits-quotas)

#### Index Symbols
Google does not seem allow the downloading of index symbols.

#### Future Features:
* Allow for updating of files and removing duplicates.

```
GooglePriceDownloader.exe --help

  -s, --symbols      (Default: GOOG) Input symbols to download. (Quoted and space separated if more than one symbol)
  -f, --file         Input symbol file list to download.  (One symbol per line)
  -d, --dir          (Default: Data) Folder to put donwloads into. (Will be created if does not exist.)
  -e, --enddate      End date of data download. (Default current date)
  -b, --begindate    End date of data download. (Default 1 year before end date)
  -r, --sort         (Default: true) Sort data by date.
  -t, --threads      (Default: 1) Number of threads for downloading.
  --help             Display this help screen.
  --version          Display version information.
  ```