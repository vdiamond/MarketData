using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using Table;
using TableCsv;
using TableExtension;


using TALib;

namespace TaCli
{
    public static class ch
    {
        public static double Sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-x));
        }
    }

    internal class Program
    {


        static void Main(string[] args)
        {
            //
            // Load Csv - convert all fields to the correct types
            var attr = new DefaultFieldAttributes();
            attr.SetDefault(TypeCode.Double);
            attr.Add(new Field("Date", TypeCode.String, true));
            IDynamicTable extractTable = new DynamicTable();
            extractTable.LoadCsv(@"d:\predict\index_extract.csv", CsvHeaderType.Header, null, attr);

            //
            // Source field definitions
            var Date = extractTable.AsList<string>("Date");

            /*
            
             gspc.Open
gspc.Low
gspc.High
gspc.Close
gspc.Volume

vix.Close
tnx.Close
dxy.Close

n225.Open
n225.Low
n225.High
n225.Close
n225.Volume
gdaxi.Open
gdaxi.Low
gdaxi.High
gdaxi.Close
gdaxi.Volume

xau.Close
bcomcl.Close
s5th.Close
s5tw.Close
advn.Close

qqq.Open
qqq.Low
qqq.High
qqq.Close
qqq.Volume

nvda.Open
nvda.Low
nvda.High
nvda.Close
nvda.Volume

aapl.Open
aapl.Low
aapl.High
aapl.Close
aapl.Volume

mu.Open
mu.Low
mu.High
mu.Close
mu.Volume

msft.Open
msft.Low
msft.High
msft.Close
msft.Volume

amzn.Open
amzn.Low
amzn.High
amzn.Close
amzn.Volume

amd.Open
amd.Low
amd.High
amd.Close
amd.Volume

googl.Open
googl.Low
googl.High
googl.Close
googl.Volume

tsla.Open
tsla.Low
tsla.High
tsla.Close
tsla.Volume

goog.Open
goog.Low
goog.High
goog.Close
goog.Volume

avgo.Open
avgo.Low
avgo.High
avgo.Close
avgo.Volume

meta.Open
meta.Low
meta.High
meta.Close
meta.Volume

wmt.Open
wmt.Low
wmt.High
wmt.Close
wmt.Volume

intc.Open
intc.Low
intc.High
intc.Close
intc.Volume

csco.Open
csco.Low
csco.High
csco.Close
csco.Volume

lrcx.Open
lrcx.Low
lrcx.High
lrcx.Close
lrcx.Volume

amat.Open
amat.Low
amat.High
amat.Close
amat.Volume

cost.Open
cost.Low
cost.High
cost.Close
cost.Volume

nflx.Open
nflx.Low
nflx.High
nflx.Close
nflx.Volume

klac.Open
klac.Low
klac.High
klac.Close
klac.Volume

pltr.Open
pltr.Low
pltr.High
pltr.Close
pltr.Volume

sndk.Open
sndk.Low
sndk.High
sndk.Close
sndk.Volume

txn.Open
txn.Low
txn.High
txn.Close
txn.Volume

mrvl.Open
mrvl.Low
mrvl.High
mrvl.Close
mrvl.Volume

lin.Open
lin.Low
lin.High
lin.Close
lin.Volume

panw.Open
panw.Low
panw.High
panw.Close
panw.Volume

qcom.Open
qcom.Low
qcom.High
qcom.Close
qcom.Volume

tmus.Open
tmus.Low
tmus.High
tmus.Close
tmus.Volume

adi.Open
adi.Low
adi.High
adi.Close
adi.Volume

pep.Open
pep.Low
pep.High
pep.Close
pep.Volume

amgn.Open
amgn.Low
amgn.High
amgn.Close
amgn.Volume

stx.Open
stx.Low
stx.High
stx.Close
stx.Volume

wdc.Open
wdc.Low
wdc.High
wdc.Close
wdc.Volume

crwd.Open
crwd.Low
crwd.High
crwd.Close
crwd.Volume

asml.Open
asml.Low
asml.High
asml.Close
asml.Volume

gild.Open
gild.Low
gild.High
gild.Close
gild.Volume

app.Open
app.Low
app.High
app.Close
app.Volume

isrg.Open
isrg.Low
isrg.High
isrg.Close
isrg.Volume

hon.Open
hon.Low
hon.High
hon.Close
hon.Volume

shop.Open
shop.Low
shop.High
shop.Close
shop.Volume

bkng.Open
bkng.Low
bkng.High
bkng.Close
bkng.Volume

sbux.Open
sbux.Low
sbux.High
sbux.Close
sbux.Volume

vrtx.Open
vrtx.Low
vrtx.High
vrtx.Close
vrtx.Volume

ftnt.Open
ftnt.Low
ftnt.High
ftnt.Close
ftnt.Volume

cdns.Open
cdns.Low
cdns.High
cdns.Close
cdns.Volume

mar.Open
mar.Low
mar.High
mar.Close
mar.Volume

adp.Open
adp.Low
adp.High
adp.Close
adp.Volume

mnst.Open
mnst.Low
mnst.High
mnst.Close
mnst.Volume

ceg.Open
ceg.Low
ceg.High
ceg.Close
ceg.Volume

adbe.Open
adbe.Low
adbe.High
adbe.Close
adbe.Volume

csx.Open
csx.Low
csx.High
csx.Close
csx.Volume

qqq.Count
qqq.Normalized
nvda.Count
nvda.Normalized
aapl.Count
aapl.Normalized
mu.Count
mu.Normalized
msft.Count
msft.Normalized
amzn.Count
amzn.Normalized
amd.Count
amd.Normalized
googl.Count
googl.Normalized
tsla.Count
tsla.Normalized
goog.Count
goog.Normalized
avgo.Count
avgo.Normalized
meta.Count
meta.Normalized
wmt.Count
wmt.Normalized
intc.Count
intc.Normalized
csco.Count
csco.Normalized
lrcx.Count
lrcx.Normalized
amat.Count
amat.Normalized
cost.Count
cost.Normalized
nflx.Count
nflx.Normalized
klac.Count
klac.Normalized
pltr.Count
pltr.Normalized
sndk.Count
sndk.Normalized
txn.Count
txn.Normalized
mrvl.Count
mrvl.Normalized
lin.Count
lin.Normalized
panw.Count
panw.Normalized
qcom.Count
qcom.Normalized
tmus.Count
tmus.Normalized
adi.Count
adi.Normalized
pep.Count
pep.Normalized
amgn.Count
amgn.Normalized
stx.Count
stx.Normalized
wdc.Count
wdc.Normalized
crwd.Count
crwd.Normalized
asml.Count
asml.Normalized
gild.Count
gild.Normalized
app.Count
app.Normalized
isrg.Count
isrg.Normalized
hon.Count
hon.Normalized
shop.Count
shop.Normalized
bkng.Count
bkng.Normalized
sbux.Count
sbux.Normalized
vrtx.Count
vrtx.Normalized
ftnt.Count
ftnt.Normalized
cdns.Count
cdns.Normalized
mar.Count
mar.Normalized
adp.Count
adp.Normalized
mnst.Count
mnst.Normalized
ceg.Count
ceg.Normalized
adbe.Count
adbe.Normalized
csx.Count
csx.Normalized

             
             
             
             
             
            */





            var spxOpen = extractTable.AsList<double>("gspc.Open");
            var spxLow = extractTable.AsList<double>("gspc.Low");
            var spxHigh = extractTable.AsList<double>("gspc.High");
            var spx = extractTable.AsList<double>("gspc.Close");

            var qqqOpen = extractTable.AsList<double>("qqq.Open");
            var qqqLow = extractTable.AsList<double>("qqq.Low");
            var qqqHigh = extractTable.AsList<double>("qqq.High");
            var qqq = extractTable.AsList<double>("qqq.Close");
            var qqqVolume = extractTable.AsList<double>("qqq.Volume");

            var vix = extractTable.AsList<double>("vix.Close");
            var tnx = extractTable.AsList<double>("tnx.Close");
            var dxy = extractTable.AsList<double>("dxy.Close");
            var n225 = extractTable.AsList<double>("n225.Close");
            var dax = extractTable.AsList<double>("gdaxi.Close");
            var xau = extractTable.AsList<double>("xau.Close");
            var bcomcl = extractTable.AsList<double>("bcomcl.Close");

            var s5th = extractTable.AsList<double>("s5th.Close");
            var s5tw = extractTable.AsList<double>("s5tw.Close");
            var advn = extractTable.AsList<double>("advn.Close");

            var qqqSent = extractTable.AsList<double>("qqq.Normalized");

            //
            // Target field definitions
            var spx_close = extractTable.columns.NewColumAsList<double>("close");
            var spx_open = extractTable.columns.NewColumAsList<double>("open");
            var spx_high = extractTable.columns.NewColumAsList<double>("high");
            var spx_low = extractTable.columns.NewColumAsList<double>("low");
            var spx_volume = extractTable.columns.NewColumAsList<double>("volume");
            var spx_volatility = extractTable.columns.NewColumAsList<double>("volatility");
            var spx_interest = extractTable.columns.NewColumAsList<double>("interest");
            var spx_dividend = extractTable.columns.NewColumAsList<double>("dividend");


            var spx_mom1 = extractTable.columns.NewColumAsList<double>("spx_mom1");
            var spx_mom5 = extractTable.columns.NewColumAsList<double>("spx_mom5");
            var spx_mom10 = extractTable.columns.NewColumAsList<double>("spx_mom10");
            var spx_mom20 = extractTable.columns.NewColumAsList<double>("spx_mom20");
            var spx_mom40 = extractTable.columns.NewColumAsList<double>("spx_mom40");
            var spx_mom80 = extractTable.columns.NewColumAsList<double>("spx_mom80");
            var spx_mom120 = extractTable.columns.NewColumAsList<double>("spx_mom120");

            var spx_macd = extractTable.columns.NewColumAsList<double>("spx_macd");
            var spx_macd_signal = extractTable.columns.NewColumAsList<double>("spx_macd_signal");
            var spx_macd_hist = extractTable.columns.NewColumAsList<double>("spx_macd_hist");

            var spx_atr14_mom1 = extractTable.columns.NewColumAsList<double>("spx_atr14_mom1");

            var vix_mom1 = extractTable.columns.NewColumAsList<double>("vix_mom1");
            var vix_mom5 = extractTable.columns.NewColumAsList<double>("vix_mom5");
            var vix_mom10 = extractTable.columns.NewColumAsList<double>("vix_mom10");
            var vix_mom20 = extractTable.columns.NewColumAsList<double>("vix_mom20");
            var vix_mom40 = extractTable.columns.NewColumAsList<double>("vix_mom40");
            var vix_mom80 = extractTable.columns.NewColumAsList<double>("vix_mom80");
            var vix_mom120 = extractTable.columns.NewColumAsList<double>("vix_mom120");

            var dxy_mom1 = extractTable.columns.NewColumAsList<double>("dxy_mom1");
            var dxy_mom5 = extractTable.columns.NewColumAsList<double>("dxy_mom5");
            var dxy_mom10 = extractTable.columns.NewColumAsList<double>("dxy_mom10");
            var dxy_mom20 = extractTable.columns.NewColumAsList<double>("dxy_mom20");
            var dxy_mom40 = extractTable.columns.NewColumAsList<double>("dxy_mom40");
            var dxy_mom80 = extractTable.columns.NewColumAsList<double>("dxy_mom80");
            var dxy_mom120 = extractTable.columns.NewColumAsList<double>("dxy_mom120");

            var n225_mom1 = extractTable.columns.NewColumAsList<double>("n225_mom1");
            var n225_mom5 = extractTable.columns.NewColumAsList<double>("n225_mom5");
            var n225_mom10 = extractTable.columns.NewColumAsList<double>("n225_mom10");
            var n225_mom20 = extractTable.columns.NewColumAsList<double>("n225_mom20");
            var n225_mom40 = extractTable.columns.NewColumAsList<double>("n225_mom40");
            var n225_mom80 = extractTable.columns.NewColumAsList<double>("n225_mom80");
            var n225_mom120 = extractTable.columns.NewColumAsList<double>("n225_mom120");

            var tnx_mom1 = extractTable.columns.NewColumAsList<double>("tnx_mom1");
            var tnx_mom5 = extractTable.columns.NewColumAsList<double>("tnx_mom5");
            var tnx_mom10 = extractTable.columns.NewColumAsList<double>("tnx_mom10");
            var tnx_mom20 = extractTable.columns.NewColumAsList<double>("tnx_mom20");
            var tnx_mom40 = extractTable.columns.NewColumAsList<double>("tnx_mom40");
            var tnx_mom80 = extractTable.columns.NewColumAsList<double>("tnx_mom80");
            var tnx_mom120 = extractTable.columns.NewColumAsList<double>("tnx_mom120");

            var dax_mom1 = extractTable.columns.NewColumAsList<double>("dax_mom1");
            var dax_mom5 = extractTable.columns.NewColumAsList<double>("dax_mom5");
            var dax_mom10 = extractTable.columns.NewColumAsList<double>("dax_mom10");
            var dax_mom20 = extractTable.columns.NewColumAsList<double>("dax_mom20");
            var dax_mom40 = extractTable.columns.NewColumAsList<double>("dax_mom40");
            var dax_mom80 = extractTable.columns.NewColumAsList<double>("dax_mom80");
            var dax_mom120 = extractTable.columns.NewColumAsList<double>("dax_mom120");

            var xau_mom1 = extractTable.columns.NewColumAsList<double>("xau_mom1");
            var xau_mom5 = extractTable.columns.NewColumAsList<double>("xau_mom5");
            var xau_mom10 = extractTable.columns.NewColumAsList<double>("xau_mom10");
            var xau_mom20 = extractTable.columns.NewColumAsList<double>("xau_mom20");
            var xau_mom40 = extractTable.columns.NewColumAsList<double>("xau_mom40");
            var xau_mom80 = extractTable.columns.NewColumAsList<double>("xau_mom80");
            var xau_mom120 = extractTable.columns.NewColumAsList<double>("xau_mom120");

            var bcomcl_mom1 = extractTable.columns.NewColumAsList<double>("bcomcl_mom1");
            var bcomcl_mom5 = extractTable.columns.NewColumAsList<double>("bcomcl_mom5");
            var bcomcl_mom10 = extractTable.columns.NewColumAsList<double>("bcomcl_mom10");
            var bcomcl_mom20 = extractTable.columns.NewColumAsList<double>("bcomcl_mom20");
            var bcomcl_mom40 = extractTable.columns.NewColumAsList<double>("bcomcl_mom40");
            var bcomcl_mom80 = extractTable.columns.NewColumAsList<double>("bcomcl_mom80");
            var bcomcl_mom120 = extractTable.columns.NewColumAsList<double>("bcomcl_mom120");

            var s5th_mom1 = extractTable.columns.NewColumAsList<double>("s5th_mom1");
            var s5th_mom5 = extractTable.columns.NewColumAsList<double>("s5th_mom5");
            var s5th_mom10 = extractTable.columns.NewColumAsList<double>("s5th_mom10");
            var s5th_mom20 = extractTable.columns.NewColumAsList<double>("s5th_mom20");
            var s5th_mom40 = extractTable.columns.NewColumAsList<double>("s5th_mom40");
            var s5th_mom80 = extractTable.columns.NewColumAsList<double>("s5th_mom80");
            var s5th_mom120 = extractTable.columns.NewColumAsList<double>("s5th_mom120");

            var s5tw_mom1 = extractTable.columns.NewColumAsList<double>("s5tw_mom1");
            var s5tw_mom5 = extractTable.columns.NewColumAsList<double>("s5tw_mom5");
            var s5tw_mom10 = extractTable.columns.NewColumAsList<double>("s5tw_mom10");
            var s5tw_mom20 = extractTable.columns.NewColumAsList<double>("s5tw_mom20");
            var s5tw_mom40 = extractTable.columns.NewColumAsList<double>("s5tw_mom40");
            var s5tw_mom80 = extractTable.columns.NewColumAsList<double>("s5tw_mom80");
            var s5tw_mom120 = extractTable.columns.NewColumAsList<double>("s5tw_mom120");

            var advn_mom1 = extractTable.columns.NewColumAsList<double>("advn_mom1");
            var advn_mom5 = extractTable.columns.NewColumAsList<double>("advn_mom5");
            var advn_mom10 = extractTable.columns.NewColumAsList<double>("advn_mom10");
            var advn_mom20 = extractTable.columns.NewColumAsList<double>("advn_mom20");
            var advn_mom40 = extractTable.columns.NewColumAsList<double>("advn_mom40");
            var advn_mom80 = extractTable.columns.NewColumAsList<double>("advn_mom80");
            var advn_mom120 = extractTable.columns.NewColumAsList<double>("advn_mom120");

            var qqq_mom1 = extractTable.columns.NewColumAsList<double>("qqq_mom1");
            var qqq_mom5 = extractTable.columns.NewColumAsList<double>("qqq_mom5");
            var qqq_mom10 = extractTable.columns.NewColumAsList<double>("qqq_mom10");
            var qqq_mom20 = extractTable.columns.NewColumAsList<double>("qqq_mom20");
            var qqq_mom40 = extractTable.columns.NewColumAsList<double>("qqq_mom40");
            var qqq_mom80 = extractTable.columns.NewColumAsList<double>("qqq_mom80");
            var qqq_mom120 = extractTable.columns.NewColumAsList<double>("qqq_mom120");

            var qqq_sent_mom1 = extractTable.columns.NewColumAsList<double>("qqq_sent_mom1");
            var qqq_vol253 = extractTable.columns.NewColumAsList<double>("qqq_vol253");
            var qqq_vol30 = extractTable.columns.NewColumAsList<double>("qqq_vol30");
            var qqq_vol5 = extractTable.columns.NewColumAsList<double>("qqq_vol5");

            var qqq_vol253_mom1 = extractTable.columns.NewColumAsList<double>("qqq_vol253_mom1");
            var qqq_vol30_mom1 = extractTable.columns.NewColumAsList<double>("qqq_vol30_mom1");
            var qqq_vol5_mom1 = extractTable.columns.NewColumAsList<double>("qqq_vol5_mom1");

            var qqq_vol5_off1 = extractTable.columns.NewColumAsList<double>("qqq_vol5_off1");
            var qqq_vol5_mom1_off1 = extractTable.columns.NewColumAsList<double>("qqq_vol5_mom1_off1");
            
            // =========================== Transformed fields ===========================

            int nc = 120;

            spx.mom(1).normalize(nc).ema(5).sigma(1.0).sub(0.5, spx_mom1);
            spx.mom(5).normalize(nc).ema(5).sigma(1.0).sub(0.5, spx_mom5);
            spx.mom(10).normalize(nc).ema(5).sigma(1.0).sub(0.5, spx_mom10);
            spx.mom(20).normalize(nc).ema(5).sigma(1.0).sub(0.5, spx_mom20);
            spx.mom(40).normalize(nc).ema(5).sigma(1.0).sub(0.5, spx_mom40);
            spx.mom(80).normalize(nc).ema(5).sigma(1.0).sub(0.5, spx_mom80);
            spx.mom(120).normalize(nc).ema(5).sigma(1.0).sub(0.5, spx_mom120);

            spx.macd(26, 12, spx_macd);
            spx.macd_signal(26, 12, 9, spx_macd_signal);
            spx.macd_hist(26, 12, 9, spx_macd_hist);

            spx.wilders_atr(spxLow, spxHigh, 14).mom(1, spx_atr14_mom1);

            qqq.mul(1.0, spx_close);
            qqqOpen.mul(1.0, spx_open);
            qqqHigh.mul(1.0, spx_high);
            qqqLow.mul(1.0, spx_low);
            qqqVolume.mul(1.0, spx_volume);
            spx.volatility(30).Calc<double, double>(x => x * VolatilityConverter.DailyToAnnual, spx_volatility);
            for (int i = 0; i < spx_interest.Count; i++)
            {
                spx_interest[i] = 0.0275;
                spx_dividend[i] = 0.0;
            }

            vix.mom(1).normalize(nc).ema(5).sigma(1.0).sub(0.5, vix_mom1);
            vix.mom(5).normalize(nc).ema(5).sigma(1.0).sub(0.5, vix_mom5);
            vix.mom(10).normalize(nc).ema(5).sigma(1.0).sub(0.5, vix_mom10);
            vix.mom(20).normalize(nc).ema(5).sigma(1.0).sub(0.5, vix_mom20);
            vix.mom(40).normalize(nc).ema(5).sigma(1.0).sub(0.5, vix_mom40);
            vix.mom(80).normalize(nc).ema(5).sigma(1.0).sub(0.5, vix_mom80);
            vix.mom(120).normalize(nc).ema(5).sigma(1.0).sub(0.5, vix_mom120);

            dxy.mom(1).normalize(nc).ema(5).sigma(1.0).sub(0.5, dxy_mom1);
            dxy.mom(5).normalize(nc).ema(5).sigma(1.0).sub(0.5, dxy_mom5);
            dxy.mom(10).normalize(nc).ema(5).sigma(1.0).sub(0.5, dxy_mom10);
            dxy.mom(20).normalize(nc).ema(5).sigma(1.0).sub(0.5, dxy_mom20);
            dxy.mom(40).normalize(nc).ema(5).sigma(1.0).sub(0.5, dxy_mom40);
            dxy.mom(80).normalize(nc).ema(5).sigma(1.0).sub(0.5, dxy_mom80);
            dxy.mom(120).normalize(nc).ema(5).sigma(1.0).sub(0.5, dxy_mom120);

            tnx.mom(1).normalize(nc).ema(5).sigma(1.0).sub(0.5, tnx_mom1);
            tnx.mom(5).normalize(nc).ema(5).sigma(1.0).sub(0.5, tnx_mom5);
            tnx.mom(10).normalize(nc).ema(5).sigma(1.0).sub(0.5, tnx_mom10);
            tnx.mom(20).normalize(nc).ema(5).sigma(1.0).sub(0.5, tnx_mom20);
            tnx.mom(40).normalize(nc).ema(5).sigma(1.0).sub(0.5, tnx_mom40);
            tnx.mom(80).normalize(nc).ema(5).sigma(1.0).sub(0.5, tnx_mom80);
            tnx.mom(120).normalize(nc).ema(5).sigma(1.0).sub(0.5, tnx_mom120);

            dax.mom(1).normalize(nc).ema(5).sigma(1.0).sub(0.5, dax_mom1);
            dax.mom(5).normalize(nc).ema(5).sigma(1.0).sub(0.5, dax_mom5);
            dax.mom(10).normalize(nc).ema(5).sigma(1.0).sub(0.5, dax_mom10);
            dax.mom(20).normalize(nc).ema(5).sigma(1.0).sub(0.5, dax_mom20);
            dax.mom(40).normalize(nc).ema(5).sigma(1.0).sub(0.5, dax_mom40);
            dax.mom(80).normalize(nc).ema(5).sigma(1.0).sub(0.5, dax_mom80);
            dax.mom(120).normalize(nc).ema(5).sigma(1.0).sub(0.5, dax_mom120);

            xau.mom(1).normalize(nc).ema(5).sigma(1.0).sub(0.5, xau_mom1);
            xau.mom(5).normalize(nc).ema(5).sigma(1.0).sub(0.5, xau_mom5);
            xau.mom(10).normalize(nc).ema(5).sigma(1.0).sub(0.5, xau_mom10);
            xau.mom(20).normalize(nc).ema(5).sigma(1.0).sub(0.5, xau_mom20);
            xau.mom(40).normalize(nc).ema(5).sigma(1.0).sub(0.5, xau_mom40);
            xau.mom(80).normalize(nc).ema(5).sigma(1.0).sub(0.5, xau_mom80);
            xau.mom(120).normalize(nc).ema(5).sigma(1.0).sub(0.5, xau_mom120);

            bcomcl.mom(1).normalize(nc).ema(5).sigma(1.0).sub(0.5, bcomcl_mom1);
            bcomcl.mom(5).normalize(nc).ema(5).sigma(1.0).sub(0.5, bcomcl_mom5);
            bcomcl.mom(10).normalize(nc).ema(5).sigma(1.0).sub(0.5, bcomcl_mom10);
            bcomcl.mom(20).normalize(nc).ema(5).sigma(1.0).sub(0.5, bcomcl_mom20);
            bcomcl.mom(40).normalize(nc).ema(5).sigma(1.0).sub(0.5, bcomcl_mom40);
            bcomcl.mom(80).normalize(nc).ema(5).sigma(1.0).sub(0.5, bcomcl_mom80);
            bcomcl.mom(120).normalize(nc).ema(5).sigma(1.0).sub(0.5, bcomcl_mom120);

            s5th.mom(1).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5th_mom1);
            s5th.mom(5).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5th_mom5);
            s5th.mom(10).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5th_mom10);
            s5th.mom(20).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5th_mom20);
            s5th.mom(40).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5th_mom40);
            s5th.mom(80).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5th_mom80);
            s5th.mom(120).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5th_mom120);

            s5tw.mom(1).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5tw_mom1);
            s5tw.mom(5).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5tw_mom5);
            s5tw.mom(10).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5tw_mom10);
            s5tw.mom(20).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5tw_mom20);
            s5tw.mom(40).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5tw_mom40);
            s5tw.mom(80).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5tw_mom80);
            s5tw.mom(120).normalize(nc).ema(5).sigma(1.0).sub(0.5, s5tw_mom120);

            advn.mom(1).normalize(nc).ema(5).sigma(1.0).sub(0.5, advn_mom1);
            advn.mom(5).normalize(nc).ema(5).sigma(1.0).sub(0.5, advn_mom5);
            advn.mom(10).normalize(nc).ema(5).sigma(1.0).sub(0.5, advn_mom10);
            advn.mom(20).normalize(nc).ema(5).sigma(1.0).sub(0.5, advn_mom20);
            advn.mom(40).normalize(nc).ema(5).sigma(1.0).sub(0.5, advn_mom40);
            advn.mom(80).normalize(nc).ema(5).sigma(1.0).sub(0.5, advn_mom80);
            advn.mom(120).normalize(nc).ema(5).sigma(1.0).sub(0.5, advn_mom120);

            n225.mom(1).normalize(nc).ema(5).sigma(1.0).sub(0.5, n225_mom1);
            n225.mom(5).normalize(nc).ema(5).sigma(1.0).sub(0.5, n225_mom5);
            n225.mom(10).normalize(nc).ema(5).sigma(1.0).sub(0.5, n225_mom10);
            n225.mom(20).normalize(nc).ema(5).sigma(1.0).sub(0.5, n225_mom20);
            n225.mom(40).normalize(nc).ema(5).sigma(1.0).sub(0.5, n225_mom40);
            n225.mom(80).normalize(nc).ema(5).sigma(1.0).sub(0.5, n225_mom80);
            n225.mom(120).normalize(nc).ema(5).sigma(1.0).sub(0.5, n225_mom120);

            qqq.mom(1).normalize(nc).ema(5).sigma(1.0).sub(0.5, qqq_mom1);
            qqq.mom(5).normalize(nc).ema(5).sigma(1.0).sub(0.5, qqq_mom5);
            qqq.mom(10).normalize(nc).ema(5).sigma(1.0).sub(0.5, qqq_mom10);
            qqq.mom(20).normalize(nc).ema(5).sigma(1.0).sub(0.5, qqq_mom20);
            qqq.mom(40).normalize(nc).ema(5).sigma(1.0).sub(0.5, qqq_mom40);
            qqq.mom(80).normalize(nc).ema(5).sigma(1.0).sub(0.5, qqq_mom80);
            qqq.mom(120).normalize(nc).ema(5).sigma(1.0).sub(0.5, qqq_mom120);

            qqqSent.mom(1, qqq_sent_mom1);
            qqq.volatility(5).mul(VolatilityConverter.DailyToAnnual, qqq_vol5);
            qqq.volatility(30).mul(VolatilityConverter.DailyToAnnual, qqq_vol30);
            qqq.volatility(253).mul(VolatilityConverter.DailyToAnnual, qqq_vol253);
 
            qqq.volatility(5).mul(VolatilityConverter.DailyToAnnual).mom(1, qqq_vol5_mom1);
            qqq.volatility(30).mul(VolatilityConverter.DailyToAnnual).mom(1, qqq_vol30_mom1);
            qqq.volatility(253).mul(VolatilityConverter.DailyToAnnual).mom(1, qqq_vol253_mom1);

            qqq_vol5.offset(-1, qqq_vol5_off1);
            qqq_vol5_mom1.offset(-1, qqq_vol5_mom1_off1);

            // Predictions ======================================================
//            for (int i = 0; i < extractTable.rows.Count; i++)
//            {
//                predict[i] = (((spx_mom1_sma4[i] / 1.243526) + Math.Tanh(ch.Sigmoid(dxy_mom1_sma5[i]))) - Math.Tanh(vix_mom1_sma5[i])) - (dxy_mom1_sma5[i] / spx_mom10_sma5[i]);
//                predict_direction[i] = predict[i] - spx_mom1_sma4[i];
//                directional_accuracy[i] = 0;
//                if (predict_direction[i] > 0 && lbl_spx_mom1_off1[i] > 0)
//                {
//                    directional_accuracy[i] = 1;
//                }
//                if (predict_direction[i] < 0 && lbl_spx_mom1_off1[i] < 0)
//                {
//                    directional_accuracy[i] = -1;
//                }
//            }

            // =================================================================

             // take out initial 260 rows
            extractTable.rows.RemoveRows(0, 260);

            //
            // Remove extract columns
            extractTable.columns.TrimToInclude(
                "Date",
                "open",
                "high",
                "low",
                "close",
                "volatility",
                "interest",
                "dividend"
            );


            extractTable.WriteCsv(@"d:\predict\index_extract_ta.csv", CsvHeaderType.Header);
 
        }


    }
}
