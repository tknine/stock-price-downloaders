# Equity Historical Data Download Projects

This command line application will download various financial data sources to CSV files. It demonstrates the use of the following using C# and .Net:

* Classes and interfaces.
* File IO
* Streams
* Lambda query extension methods
* Webclient
* Threading and semaphores
* LumenWorks.Framework.IO.Csv parsing library usage
* CommandLine library usage


## Yahoo Historical Data

**YahooPriceDownloader** will download any number of specified symbols of data. This data will in a non-split adjusted format where the **Adjusted Close** is the last field of data on each record.  using "-a" option will use the Adjusted Close value to adjust all the price values in each record, but the Adjusted Close that comes with the CSV download is different than the adjusted close that appears in the web site version of the data.  Given this issue, the data in the CSV download will be off once the Adjusted Cloe is applied to the unadjusted prices.  The unadjusted prices do give an accurate way to test against the real prices of the time, but make it difficult to back test against as the splits and dividends will cause sudden price changes in the data.

Merging of new data will overwrite records that already exist in the data file.  Records not included in the new data download will not be affected.

#### Index Symbols
These can usually be downloaded through the API.

#### Volume Back Adjusting
Volume values are never back adjusted on the data services.  It is possible to do so, but it could cause the creation of volume values that exceed the maximum value of an **int**, and so the volume would have to be represented as a floating point number.

#### Future Features:
* Download splits
* Download dividends
* Auto apply splits and dividends to the unadjusted data to get prices that match Google finance and yahoo's own web interface for historical data.


```
findata-dl yahoo --help

  -d, --dir             (Default: YahooDailyData) Folder to put downloads into. (Will be created if does not exist.)
  -t, --threads         (Default: 5) Number of threads for downloading.
  -a, --adjust          (Default: false) Back adjust all data based on Adjusted Close.
  -k, --removeadjust    (Default: true) Remove the adjusted close from the data.
  -n, --round           (Default: true) Round decimal places.
  -c, --digits          (Default: 2) Round round decimal places digits.
  -s, --symbols         (Default: GOOG) Input symbols to download. (Quoted and space separated if more than one symbol)
  -f, --file            Input symbol file list to download.  (One symbol per line)
  -e, --enddate         End date of data download. (Default current date)
  -b, --begindate       End date of data download. (Default 1 year before end date)
  -r, --sort            (Default: true) Sort data by date.
  -m, --merge           (Default: true) Merge new data with existing data if data file already exists.  New data overrides existing data.
  -g, --log             (Default: --download.log) Log actions and errors to specified file.)
  -x, --dateformat      (Default: MM/dd/yyyy) Date format. (MM=months, dd=days, yyyy|yy=years)
  -p, --sep             (Default: ,) Field separator.
  --help                Display this help screen.
  --version             Display version information.
```


## Google Historical Data

The google data is a good source for US price back adjusted data.  The data prior to 2000 has the Open price for each record zeroed out.

Merging of new data will overwrite records that already exist in the data file.  Records not included in the new data download will not be affected.

NOTE: There are apparently quite a few of the symbols that do not have a full history on the Google servers.  An example of this is Alcoa (AA) which should have data back to the 1970s, but only has back to 10/2016 on the Google servers.

#### Request Limits
One catch is that google implements a request limit based on time.  This limit equates to about 1 request per second, so any data downloading on the finance API requires following this rule.  Read about the limits here: [Google API](https://developers.google.com/analytics/devguides/reporting/core/v3/limits-quotas)

As a result of these request limits, the download executes request on a single thread to prevent an IP lockout period that would occur by letting too many requests happen per second.

#### Index Symbols
Google does not seem allow the downloading of index symbols.

#### Future Features:
* Allow the update of Google data **Open** price for data prior to 2000 by using Yahoo data files to estimate the Google data **Open** price based on the Yahoo data non-split-adjusted **Open and Close** prices.

```
findata-dl google --help

  -d, --dir           (Default: GoogleDailyData) Folder to put downloads into. (Will be created if does not exist.)
  -t, --threads       (Default: 1) Number of threads for downloading.
  -n, --round         (Default: false) Round decimal places.
  -c, --digits        (Default: 2) Round round decimal places digits.
  -s, --symbols       (Default: GOOG) Input symbols to download. (Quoted and space separated if more than one symbol)
  -f, --file          Input symbol file list to download.  (One symbol per line)
  -e, --enddate       End date of data download. (Default current date)
  -b, --begindate     End date of data download. (Default 1 year before end date)
  -r, --sort          (Default: true) Sort data by date.
  -m, --merge         (Default: true) Merge new data with existing data if data file already exists.  New data overrides existing data.
  -g, --log           (Default: --download.log) Log actions and errors to specified file.
  -x, --dateformat    (Default: MM/dd/yyyy) Date format. (MM=months, dd=days, yyyy|yy=years)
  -p, --sep           (Default: ,) Field separator.
  --help              Display this help screen.
  --version           Display version information.
  ```