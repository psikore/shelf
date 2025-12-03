using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;    // LosFormatter

namespace VSDecrypt
{
    public static class VSDecryptor
    {
        // url-encoded base64
        public static string RawViewStateUrlEncodedBase64 = "4SV4jB9Nn0dWNugo5Bz1dEd9HVjBNCfV81Iqo%2FGL8SEg%2FzMACn3Jl2fnBaahfBlHiheoVa9DeOskSQLcq1%2FNWO4t9MFJgyzdi5P8bV18tMf14rmO9NPhXkhUIjgp0CPMWQIINxgxqkT3qvPC5V0UFABW2s9jsUP5XRA1g1PyRaIMdfmBVVWtExRZLEAWnfcFQXK4GX0NDz6jhil9DKWfD7M2EdzLRunBiuFCZNLPPsxYJ8%2FaOVh6aOHuygwkkrmAVjtIw5VxkW9OVkRXO0JbQoqKpzhPDFzkHCq8Cs9YG5ivhqIAdpWPF%2BQXZMJy%2FwwjbweV1R2sDCnd8Kgkr3%2FovPuJDN78dEreGpqGbEvmp8R%2FrgVb90vPvX9atdP%2BUlAJzKVvzn1vY4qYvGjKadrQ9B2g42uTo5gUpJOCNVPpGgOrQDWL3pt%2BHVUsQxLXT6kJ1rAVgoSiHQM9xbDQD4UDCTdELmt3oMWHvEbBmLdiZL8dTcKGY3xm5UxiSVCjemeNGsmN0KEL9rRyG99DaLt4im2TvT9MSE1xT8s30fcHvUkrCMBiLr%2Bqo3apilgGWQAY3f%2FsTzGgAefJEjnQZKeXANNFIVKhj0oRsyLKQCownjieDxHujw0qKsU5oSY%2BOsAObDUCl1%2FNAJPNEUF2ZGnuhu9KiUOSEjPFFsCYIl3v3gMnwjV%2FN6%2Fj20oyenb5SdD6FodVz8vGbb9K8bgVcEPZotWeAz%2FbRXL8SmxaWot111kJtCzJuun3A%2FSnHSXuUTRAASZh4AkuN6gEN7RmZnGxMQrYHmC2onfwnuSNIfR5A3PGBpG6aqKGftwMdeTbpJxbQZSX99jqOSIoWySGLiu7Hyaa%2BJXGHr6343qgWn3BRCvkxdKZ8P4%2BFIahr5cIXwgM%2F%2FI83al3Y3NXKKksbqCBGEqbUd75msMERCJqojhiHTEKxZS%2FlCh%2F1gZFjwCmozAUX1uR48leMTpIxLFKC73QniAaP7HMRW%2BsDsdex0Pq1VGjJY5GRvvnJUsphTUSr0LkPoX7Em1lQDFpv%2BRsY5K7N3EdVgaiTVDpPvluiq3nlBEYNj2vkeSIrH1235st0Km0sbjdKZKl4OerYFkBXff7%2FgyOOy6Kxa9LLkjqrsPE8VFVrk6g61Ffrz6%2FleZ2EpoG6Nu%2FB7zeQ4gHaYR6jyZIcnybMbdHNP59%2FpBH4UDhWjteYpOpnaF6xBSRXhlb5MqzGcXMNzJsUM6gfuodm00u%2BejxmYzx7fH9C4zxDElQ0UpUA%2BUVQ6%2BIOF5zj4Ja7DuJgsncjaVU58jAVv9f0Bpvx%2BzIVOzsknU7HDrRFG46mzhfP1o4QJYncxvbmaM9A0AKM8GJmx0KIc2KrdsaNsMU4KzvSk6GjVTwkjd35FgLhMUFJbYI4LPMBxHinfbXwBN4Zmaj9AP42BG%2FgqO4woECZaTd3qEPTriZtRd0bLyFmgry2vFTjpusCGCq2IiDzTexaXb7erluzp%2Fq4hDIJc5wwZay821uNiUu8JZ%2BtXPagF%2BlVG9zleiaib8AVVfOD1EHCNy632%2FohPCipl4ENVbcyh%2FwGIx4sJdCjT4xWqqCv7W3DZXyz6yjxIxhJjFU0klv4k8yuF81vMMhlstwFa2%2BMrplfqKUeKljLfvCihAO842FW3z8C1s7gsySswbSO9vlOBYd0oRhO0AG17msNQsRDb3FgzHgPt9duXfysF4Z5GsMMQsUWY80xw0KEQNUay3hLkr6pO8M4SfQhJXX%2FDOaTvJzUWam8oACcwdW7LB4eS%2BK3qqA8ouURqFm%2Bxy%2BbszsephyuQMfXp9r89eQPP6slD%2FxQxFqjguvJk%2Fi053QejcmmWaTGIqk7FvZw6KhbgKyAYJtHFkJqi1mUuJUZSGfLpT0B6ySiaFESOJIDeUN05Uw%2BDnNiLtMgbHODsdZige4CL5rji%2FYVvRVVUN9M1wizqZYw2L3TQ9CYaskTzxPeKi9P7in96V69v6ge747AH3dctDEAUKTtufzIdVhoQEcTphA%2FyWSjaBXXHdE9Rrky2x8Jh5LwlhYHKeL4NzU5C1uWOdgZeP7LZpAg5tvwNo9%2BixvADuqFtFQyZr%2F%2BA4QrYI9tI26IaUXPPi%2BrPFzewGCUXYi%2BSe5%2BjPLdRfO0cAbQhqMExQI5QYbCA1yWGd%2BAQaqwJMACYqfaGin8hlaEaUWyIkhcPcxKoJJgiCD5cPyM9o8AeCJ89Zedy0nhtZkDeAc1U1J2tXsytgd4NtB11URiQKYgOqcBZ5iTm1GZ4DgXgimbH3mu9giAaoTsXOgbXeP33jDG23gaJbp9BEURVcC%2BpDiAPaos9uT0MK2KU4PjL%2FfAdBaCsEPkPIyIBZjkjKGRQ87g5OZFiat3WIUTGe9%2FoPMAePvA4huNeLSNmNi0FWgNRm3oDMnJYr3Rd0eClm35Dc2P3wVaZwIV3Q9XNSPpJxPzV9VRI6nz1uxvJ5tGbtQg%2FuYaxYAJXYQDIH2JWXdyw4BU3nEDey3FVHtejK%2BwlA8dC%2FG6SoBfBCrdapBB%2F4tsacMvkZPFT59WhH0cAslT6xeKCRO5ZItPt3uX2ES3CiLO%2BEFMbr3y459hyqRKPYXPY6cYb3eVBQU4ksJp4%2BxFvjoYXflZrxKu%2BkJPF8yAeA3NWaahQWhBjT1PXYPpGc2yodtoXK3S4HOZcw5r0jL9li22g8LjAqoXbkBvbSX8xJswmwnju0fY2wUHpkur4j2ffLD9qcfCEaQTUuLPO1NK8dFeFqW2ZTfSYSPhwAh9GycUiS5eyhQvc6wY2jVHra9NZNb%2FiotV5WwPD9o42jasGLsWut6V91jFRCIH%2FDeYno9twUDO21pFz2z8gI4Z%2FNjU%2BCfy9vt%2B2SDUuPwk5R98cAG9mDE4gndDKHf%2FZd8iudanHncPq39fWZI5f%2BaN%2BOrwsEfSBTmdDxsRyc5KQDnHMxXpaNchLhEqlb9JE5qEZ6GV1g2QncHSivoegY2BIgWIdDzkZ8qyia6q5r2hvexGsINxEsJpo%2BEYFbHqrjYJQSi3RSFRunP6zvI%2FbI4lijitnFjaVA0OULlM7lWNt6Eg7MSTwbpx2cYMmHle8GcnsA2EXF887sKrUZW5pZu79gDXs8ScXWPblU%2BgY59I1tN%2B7vPo2msskFiehbGerLxTtEuTXp9dsdVr%2BCpW%2FXTHD2A74BusOJ8YEAQWRUZ3GCZnIhni5qdHA8sFb7zz9Q4FXM6bgUW5un6vXQQPW%2B9HtY%2BuDbEOg%2B%2FcdSsd31mLG6Zp3DzOlgdOkAwCV2GtbGsaB5fUr9AX7tC%2BpHbwtVHmU52etplXaYuIvX71gt2YUnR72hIveBChKqFPchnD2GStdPVsuu%2F84bdO%2B166ES6B1iTkqgRWf2hX%2BDUe6NjjdsVPx6%2BZr9frcvL0JUml8F8WOIXjkZkdFN3P8DOl%2FNgEy%2Fi6S0fTom9aVpiUcsYVWQWcN0uc6zdOAfUcterxKBA%2BgXtk8NBSbcip7Dl7FNisqnLMn3%2FET84u9pAGDfieWSSKr6Ma3rFjzARonY8b%2Bh5%2BOezk2YqCw3g1KK5J866%2B9QpVnNeErRa8H03NMkCYV%2Bu2O3WL1Fc1X5Ky81u8JV32d1srhOoikPdfRAxvVjFBVfqmdhDiyiLwbOn4YT0QJyHKI8KzBVCYLRnuQyEqHo1ib0SZAHUnxBmhNVdFMsJcNkKUSmCFvWZoaf5tgNFBmRS97Ew3949hjjoVyhPHnr9SdSoUSmmKyjISdV1WqWAMcyyxwHcrgdArQx3iLwBLZQ8ktVPAqexkiqj6UfSVIxozmRN2XphksyXJEldc19sIqpu%2F0KZAbpXlseQVEG%2FW1VenZ5pGKM6vr9bZkjMXGfQLB%2F5MnylFI9CK0xrNQb3ESi7iO3f0aptok0h%2FvPX%2BFNrXgid4Oi3a6hvL7TvRXIhY8XxVwQOYntDn5yqYTUkIIRWAp5ZrwAEy0%2FGJAf98fn3n7Y2tQGEZAAhXJAylyYldjcd5jcU4bdiBJkG7lGkRi3gU7kwfAdMAyYrOUwxNh4cD5xoTjNdBnJvQDYENEncAtBXaktOem6tFPen17kaGUCpDKsVWmhVwjF5sIUtec09e1x6Uku8BilI4RFfr4XQ27eR4sdGwdROtaTMzJf4gegx8ImWspMEzFs2y3xgyjjTK8zdPLXAIBSu9T8c4T6J2YxvxNQ2w7orEgxko0K4X3OLVcRB0oxATRjVaFpVGyKbsEXppjK2Bm2pRVDVEvKbMP2cf58hvZMunoXYpTG0%2FL4Y6RlDX8p7F0V1AfEMbQhhnk5YkQTSeZVnRMUo4jouog95addiEOfd2yJBhVvyb%2Bd5ZQF3dAEy%2FHjbkMlDRBX75g%2FJAdjtnhpu2OW86c3iXpgnq6Yj687%2BKE4xgOFcsWZwvLwVut404F2gwAy%2F24IvroHJhJZuDtV1qLHX3k5F6i1TwWrV6tcm9%2FwL68wnZdo5HVU%2Fo5PsFZ50dLielCtcd36q36Eb6Kb633AliERrGuDrdYc3Xsm%2Fa%2BgkfkqzpVhuBQI96rtRYadvVQpdT0hd6mhlxF1atwN4D4ocS2IvW4p8E%2BLtec2zpQvHWJEYEI98Feqn7M8UqyN%2FMto2pSWeJWvPcGS6jvQ51JfI%2BWHQdOORfyWUgiDB9wKBdm%2FZoBb4xFgg4lvsq5CvuXSqUuF90q90iSQXz9P5Uy3Se49M6MIt%2FMH0YKMUUcW0Fv0vh3xFCN8jUR97qfMNZ9zY2tbMquUD9iKQqzmZd1MXnIWaQd%2BCOyXypPdkNrk%2BgzbW8XMtKjhzJ0HGdVgw2IAcrFBzlZ4WJxsyt59pqEfKsZVSO45noJuFJB9y%2FzJmsWKahlktPlt%2Fw0xGzOk1O0HWT8vO%2FDip%2FpDbBKZH0VNGS77W9AsOzYPSL%2FFtedkyNXliVW6GMctnjtLzv%2BY5ND5WGg2uP3ZNm2ivthtuKPI2AR3Mf6xgMnRIaVzfTR3MVar0POFvZJiiFtDOC6jRID8NVKlo2PY1aaaE5HdN%2BubxmEOJB05OVFobA5RtVjKTIbmk0IvTLqr7t1laSJ%2B0wVG7k3tzpUXok4k1D%2FVVKBO0P2omyZ4VssP1guRmAepsmF8fepIoMCTaWzQCCQnOU%2BiS1xVn8VHz3%2BOScTEH8BTNyB2%2F62Pez1poqcSrziNaTsCu9d3c1orBx7YlLxMOFytrQimRxpfFB0xPZdASvu4DbCnXBSOjifMt80v7Rp27cItZEuyPhUlrQ8w3t2h655egy%2BhrTbtdOOz69x%2F6xx%2Fm3wHBgiod3QL%2BkR1m6Vna2RseVvG4OV8fzdBskjqVfwZI%2BZqEedTx2Y%2BaJ%2BLI6FE1tgWQvy5h51VBdW6rB7pTBjIfum1wLvu8RDxEzQ8NQGvguOExZ5vNElh4vM2cFDh4qtx0RrN9PnTIvIXg0wjsXM4WjVZLP0pTKRaiM75cWBHH5%2Foo85vpT%2Fq4R7nrx96LBz3vgy3QH%2F%2BHByDLI%2B8mOAifDgx7VHvOwwvSdB%2FQwI37XyLTtsh%2FcPONw59UOzX8UbA1hp0NKkww6zzUycAUQ2HDaoDaH%2FCrR%2BKdc3evVpIbyFwtuUicuHkppokRMZLw1YEGlB5MM2eyrWVs5%2BpcXFIRNY04ZLnGFg9CYeDMTwCfA6lO%2B6gsxc0KMr%2Bb955pQ%2F1pHELhlE1mtU5CncdnWvDtOeRL8eMv0YG938KLOmg%2BscqHs%2FTkZz2kMo9T3S55VstnEUx8nIq%2BIfFUU%2BXStijZb%2BBvDwhGukqmylWBOKJ85ya3qYZNpQ4d4IR7CjMFUqCYjh6k28uiuLJDa8p2QyXaw5dYdvGSvjWGa94%2BjMS6GUrTb1ky0Z4OtNjDVZ8j9K9R3nP0AHfXNK%2FToi99%2FnVRfgcoaAJh2FKakMz0FYVJ61zFsblxXZiRkvzWvzLJ8poDSZFClOwCutY%2BrszYetY7PYmFPvdqgYdbtNBB8MqnQCUjJVWQtR6eXQgbEnls0GarGfa0hAV7nzD%2BM2Az9diuXjMfbLIv9lul1%2BxemcZCat7X9lSVNRtc%2FA4A3%2Fx5VpEjE3zrxvqoNzJmlv2roJIRgQiwNgGPc%2FMgm0jLes25BGqNWcGwJVyVf%2F2xLbzo7yDAFIgoNOOTytzxFMY01Ty2jx9UOnuhINfKcLS%2BGq6MZQpUai5HzY04EYKJkR4phbYe3hmpGChQICNeFiDTogkpCXyshSsLIlpqZ8Ujva9cOB4PaViEhLHfuwyM0tVT0vR3wcfT8DHNtjK2WWQ32rUpkmpum3ZO00cWh7OHlpqrATs1ifDKdVd%2FSTojbTx70ja%2FYtuSb8o1pplkMRkqSX0plQ7KxNEdiLOPxdt3HGg%2BinrI6fxHa38IV5HHDij%2FYYvdroGzEp05rsDgnP4pnRh2Px6kdrrt24VbjlWUQXAmtw2iIyU9jzH4GrnKEF3djS4nDDrquiqDpWZ0wy%2FpgVUWF3m9SL%2FcDed8v6sN18K6Vlr7mTUsFavNs%2Fy9kBRPlJ2d0mMimz3MuFctbyfxk28F1KFz6HeOIVwWeaS%2BRrLpnT0Wf21Qv%2BkQcBEbdUNGSRpeSxR%2BKYm5%2BjLMFs6SdyBudY8yALr6DbcogzpvqDVC08H2BZ21yJ6%2Fvnq2fZOvJZme7bva2WOVKmmv0zFT4jo9JNFabGaXc%2BsPP0%2B%2FITzyqFyeWlU11UaopfJXgMseHDTh%2BzFkm1bBf1XYudHy1eORpQcId9G8EGRnayS4uLdolO7lx9kbyTcaNnRUz%2FSW6aW%2FFu7Zp%2F8LLikvc9AQ%2BFzX6Uu%2F3BE68KYcyBizD0U3l3Zie1Jf%2FHrCUYRRou7mliiTnICos8NnjAkw2Qju8e5wx3SYOvMhf02CuizDK9PPed2KyTfPKtrUwREzvBPZC18OSiIjd0QcYIozRXkErHolCBeZSj3IgsnZquYrUoTyXVSHkXlP%2FgIlipBbiRR0sm5C10NxZp6M%2BwVtZPlh7ZXmOHfIjtASPSfJyPM4gZ9yV%2FXEdpuPBeHRhHwZE3BmHLfrzWNgkHo0O%2BdlHTZACNkilCelV6gCbp3dbkvlWWMV5jP%2BAksSu7m3ICKiSJLRwJ2UsMnwQwSYhK2HQvXZjrh8SDwJ%2BS%2B9H4VepyaHn%2FG6a6jS58pgfir7dYNVt%2F2KOAYCo50k74QDf2iL0JRQQB25I5UF74uhiPaLDvzFwdfWo7W3mcCECTQm0FUY0E8ePhVFMC2jsRDeNP6qqG%2FDKDEvKBLOOxM7QWOnStW6cLBGJXHTAAr8xVMUeMwJeYjVeQh3b6tMk2mVfW4E1PnRU7R32Wgb8CuUWbvNyYqoZC3v8ck6B9wMipIFsGxWGtYfZzdk71iQnbWSuEyPJ88WffFnitepF8bEmeJ%2FHf4%2FhpLlYNsbU5uaaAVt8rqScMJzeJohcFZsfoYf3Vh2x8pEfkvypiIYSCurxN9ApF0ShjmJlvs6635PZStdGaPfQlILJjBHxehEYh%2FIKGkFbWlWUv%2Bte70IU28FznxrWGwWyyCbbeb%2Btt%2BO3fd8PBOdK3DXWPYnVq2xcLYGMOvK99k3u5m%2FKUu1xEZ9ZGUuMP6uVcavjXhPTUKchfD8JcXzNJzeaAutsFgm%2B3lx800HmY9TY%2BaVKMHDVJizze3y6sUXMN6%2FMb0o98w2Y7Bf23Rs%2FpyRCZ1G4hgvTmukWya50jIo4tRneWBttrv3ZoDS4MAME2E1aO1X%2FfSlWKeDQjxZlU%2FGZh4Sxh7wB8jW5sxrF3IXtqzZzLptSLkgUzxvuKHLnmYNqBZoiFEi%2Bm0Mn0yqIcFil4YuulRSAEpoXurUqYOsfx891yAsRC5sAehsl3qUzg0dx7rVxWVoqYTucxIV44vvNf2uSfdl1j91YuTIisxl6NcwASTt%2FpESItzu9%2Fgoykz4PW%2FLIMDJbzVK9OcGxQve4v1%2FL2UXVnkFGQRcme5b7syZ2R8Osqsk4EbGHjmy%2Ff8c7OK6LV6O%2FZKVNin6kuJPlrw0GQT5sbDUTffuA8L7Gm2Drsz5JFXUHgqthzuKM%2FEBrY69oZ%2FE3w5ru2J%2BNKKLbgkOgyblVxgYIuHf3eScHJHBYvqxPNii9UiQM3HBHD1AZustt3fl4OdsDTIY9Ksw2%2FEMRQakiQ%2FeUw0vHsU03L6GKAENkNQrFUhlam7EQe2uSWodXTIHbnt0j22AvxYrgBUDbc%2FDgeCD3q08jF8V5553gMMMHRuX2muVSY0NtURDzFkNIEmIXKgANLT4SvzUUhfOumh6SxaPgZGwx%2FpyIgJdRwzQ%2Bg0kApIM9kl3bKG%2FnBZA%2Bh5iLXepUtHK0TouQafMGD3K07Qz4Dibn1JIABybKbEfz56oU7adq96HC0jLLBUKccPoZto3ItMMxVNuKyqoUMG%2FSYRJjMXDydTym8HPvHffYEYXupShT3VwORsWfkPcUqLZ42l4h7VtLyPLP4bTFXRnroMb0RBxZxz0BzgNFBHG3niBczBQD%2FkZa3%2FvTmFO3OKIaWdFybMhBF6NQUPST4McmloD%2FwdT9S7SEudbdVH1UhYy5It1Msog2mGWV4QcN6d2fLPeftOcSuxnRxOWP4HXYObpwxu3WnKISU38cU1H2S5HVNPtWpZeTRqF33iYodTxdF3rLj%2F7lMkSf80AobNuKXxB7i2da%2FRU0knhTmAvaA8uHbiH8vRirYTvDLM1E3pq8SKFMg4LdqFbVMu3Y3%2FApIj%2Bh8pMvGtwyjoQbhas4xT9r8c9t3u4fjvcW7NLgQwRiNNtxxBGJhOeIRt0KJFYuqA5JX23jqkEBsjSIBwirYM9TSEbTn6COJp832WqEiteUvrlxJmH0Uy0m4mcLSIFnZ5JUzWhJ1jZCGWDTBtbBYzxRVLumvpGcWSGucHtmUNtWUUCEmK0Xtpq3ZTEVRY9AA7GBwE0Is%2Fl%2BA%2BdwTRMo5ktLWjPjNGWuXOFII3QhS7Nf5mx0LGCGWN%2BAd%2FUGcwBpwxd%2BE9iduqn53Na3PygEq5HOtY5ESuU7gpNf9zBNNDAuNy%2FAVAq5BIL8%2F%2BdWnKMzWajIdXt4grKh40GMMoDyqH7uR7R95LC0a9vrFLyUppm0Eg8hVEFOD67sq5YsMS0RgJ1uGVFUWElbq004F0KTMW5ZVrnK1bxrbH71LBKouVxDUkEqHfsS0ex4sjzmbJdtnP4dINMePVmwIDhXlUMd%2FLPFz7B1QgTfFdRNXDdLIpHzc4gwfpuVQl6ZSsJHAS9WYnuQkvSEDL7Fl2tNA18MbQGFoJuUwQ63xs85MAaTBxUm%2BvsEdFS3HmgBrkzTI8T5tw09HfSqeAbf427jp%2BbHQIJNQSKINeyifj%2FnBBjuEJ9VL%2BpuEW2%2BCpkN96x3XAM2Qug%2FTG1iksNhg0bD%2BDI8QmxbxMmcsEmHoRXy2553n1ug%2FkU26aroFY3thve3sMxyXDvnz0iBH%2Bs8CuAXr75ILVUNlXdCt%2FhQ7466uRg0Y457PW5aYgXf5Eh%2Br4fjbrfsO4nMVDDmqGy6d4kverArPwJFW%2F3Tu9LYjUyTCL8FrvE5TWTSoXYhPnVOZhV6l26Jj7qrjZHwEMAFPZ%2B2Zoey3qmDe29C%2ByFIydVeb71ICZnzGumhXYHFp4c8wpsbM9YETcN16LHZigp2hBlOtNimE6ActzWpcJiYScp2eRZuEShcf8YkMiEO3qdWPk%2F3p741zjguSXqqUh0eifs1P%2Fk0Q1u2Xmq7cCIb0jwaPaPDyjIH5%2BSXzsxuHsFzq%2FA339LLzpQdPFrhRl4n9mAT5qbXZoMB6W6cqVNlosxdIXMIzlP9mWYgVPHZM5PTMiXQ9eGK2V4fO3r2PIoUwHdCK48wW0vDy4o%2F3%2F2GseK%2BUx6zbnVd6gC9UaRg6tfGOrHsNroeydDR7dUCFIsoJ1zlSMX3LduT7291rcZTLu25Uu8G2wTGu8XZ9YhGEVHTr%2FQ61qvrMwWDlIZcsz%2FOcPvaUHiduvv6i4OQXysezxmAysyMy2IkOQMLy3hJRiw2jecu%2B8EfcoCmUcZmpOGMrC4SvOQqFfBVKjWcQsD0i2TEtCmDhDxG1Za4QIpo2Rz9Swuti2gppdpTKovA2TfhGW4J4QU%2FQtDZLBW3%2FAVSAKjoALbu%2FczxQIv4%2BtRmttD0Npdj%2FRyoD6Yyfr8tR9XbIc8vut6wtzHbvsjtERrtdswOj78TH3jZCgL1GKf4PEuuLQdmxpIK%2BrLoKI4TOi5iXcTCLPp8Skgov3lD4fuYtWIQVOgG17DqwX%2BCnSHI8sSdbg8VRpnUTzxtK%2BGUImfT2RZCQRLI%2F53q68IB%2FKKB7FrjzEjttuXXZHuodhIB0FzqEqoOZEAB1MS4%2FgVzwLKgiiKUXtQB%2BdOAcHmxdvVIYAFl%2Bsof%2BSCXeslb8Z0WH8IBnB%2FYXSffNwFH1%2B26l1tTJxNcvagctpbXCkx5o7aUxhX2ne9vSL1oYGZ1%2FTuA6mptLRPG5VzJg96wSlEg9oHSzEl%2BxuNtNwwPMYa6o%2B6APHrzDiN2M57b5mH9uQcipMsg9min9c%2FRHELmlrNUmneHTkuP2dc5rDe6WMisNYZ3LkcZhyywDKy%2BrnjQdEt0%2FwNmxJca4fhbVrhgDn7EVpvvsgGqZ9cHHl4CoGqIMOceLPKW4T5zExCtvKYFEXDC1rbekj83xjWK3oKV8itr88xKMo7kybeBopCDOIr7Rzm%2BZ6yYpEr3wmqCawwuOwOZI9WYBCFXCEV1UEeX31x3SmMRHlweyIgQSD6XbWPjGsaPy%2B6rhUvIc8LZGfsIWqJM04I1EN1GfWkPChxoTTUqFZiV6UUOlvhYR2uG3DoHvmSmLS1%2BijHIDSwTiuz96qhTwSJmEsrFEExkhVdy30eSnh7DvTx3vwoIR7IcaRnCSY0foLwHrm0H%2Bhfkrbts7JlSptIhs2GFDPLsdJJ5JPnP%2FIDGDe9kgGPshAVROm6C6Amh5Aplr4e1wvVsvXTWzwWurd2%2B3ybGE2iY%2BWk3AVJ%2FM6z6uz0VYq5UtnkAqWmn%2B%2B8yPPaRjFb%2FrQoCmNRUsPu0wzsp6ZMbMMpwx8PEnmf%2BsA62rdBT2NbC4nyYivH%2FfpTqEUQMzS40uD6F2RuBOYxX8sx%2F4N00G%2FG4GFadgwMp8U1HBXo1SsG9d9YpuzxMrxS%2FXwHlA3b9XudWhBZ0N7ToJ7YoI4bDRKrT6D3eigarckLNf2hyFtwAF1rMnXm5LrOZrDMchUs6n2AP3QtR1TpXHL%2FTxo%2BGvhDR5DJkVQcOfYugNNUsYoM0Jr%2BM0e0jIoCj6PB7WUtya0%2BEAimqeivB3NBbNBSRIup1Bho2LzLrx5CH75gIXYGSIYxIpDeofvsGcpugW7CyLhcYAEDp76A81zUQFsZhWxDbtsV2sBsgMXIIbU8lx7Eo4l91%2B6YYQQAsyfsJ%2FVli7vBCN%2FLucBu5AcjKEZ%2FYz3FL93QDcPXG%2F29I7IUL%2B%2BNsF3vQK910xB3CUz4FqmQotjU49ClCjks%2B8cV2GyTjbZbaWJF2Q3l4LmLgl%2BSgLZoeQVuIBEtnnd0GwRzddDzCPnpkwsmbpRTRd3qR2aDarKDasnlU6FThKqPHlvc90oFKzBlurfgaeBXID3xLBKmPgNYardGT13%2B%2FoHsKUsGPaKc%2BL0AtoAxxaWMLFbSS9ODDj0Sl69L8z5gwBm8QP%2Fqf4cRAimMHoFDALKt1YPQ8VFznjWosY%2BDz6NiQokv8WJ7%2FG%2Bg4A8YrirRHXcNbHNaKyTPZZ2W5EK1IOhc8XrL9krfjoYXETEJaDYYUYIx1F7Osa4dIUJEdeW1N0OYYhOwT%2B5ADjsDhrLqx3SXHIzjHNn1ydsiarx6%2FUcxsb8pju%2BXn3gohOFVGqVbt7h5zHbb5miZ1CqEJR7BQ1XmFhJL2mCtjoyYOLot11Kz1WrIK6IT0GLtVVA4lp9MtoBufML7Brg9j3B7a57krEFfkb3Rd6oQT3bdSmOCvKkXf%2BGPv0pFfMomP2XUd7AW67CkSK6XFEg0YwqRWK05DpkjPKrBpi%2Fee1Oek3CrKFiENyuPc2oTDttNg41tY5oAjCMzMi2c5IbS72BqzL6JCuzha6mib2iUZUVrRPKm%2BhgO3A28XKntCYs%2BsaHarV05yyortNYoOfTE8f2AkEdmvR1iMsMsOoW6PcL5mqmpL21%2FSsOyn9n8JocDTLAYOTgxD058hKTZpJztKOA1%2FzpaZ2qCTms0BxP6krPfCdxc9UgJtwv6FQUNKhBgwBzLtoF6P3%2FMchy5JlEsntgpZLElIVLMxB2mNhV6OCMwiCCr1BDZ00QJyAW2Q7%2FXkR5aJxXBM%2BP9W7fiHRvRM5mK2Mz3DU2vd571453%2BdjnArgQDpVazzgahqFFJQcR9ZMbgX1O%2B1gQI%2BPp3pjCYTMMSeKBfLdPpclPoEGcgTf9HntmqUPLWUDjWvifiEpAq6TgcHm3mA9NEOBu%2BfYdegS7sf9WXN9UOLjKx%2FlmyWE1al9eTX%2FwEPVFnQq3fhHJwOCUwi8%2BiOO0SycYcucLfEntxsen1wBebgQHgT6Hp54tuLmZkKVz%2BXWtcpPnSDghU8zWwI9eQZb%2B3vwsrNRo8MRk%2BPV%2Bb35P5cSLvWcRpAIBMIZaFw1ladQoopOnRf1yzZ%2FzRkISYcqU6tHb4aSYcQauNnVXMveY2%2Bsuy6GpOm7Q5h5k4uSN66l6bA8xP8uzUt02gyQHjg4kLs9rDxp0VVNRj5yROTbdEE7mxOkSs7pwO0QFzaQRhWLn5j%2Bd0xCJggLmDUG%2FX5LAC9ktlK2acVf06yB9HTA6Kza%2FniIssMHkOoUh6zmydaLvcdD2qP8Bxtq%2BzjfDsONmdj5eRn8IlAjUDC1GZLsyIw3veO8xTXIKjBfvMfseFAgEM9M7eeb2OLI%2FllYAUcGssGGiG8M9c4bf3OONIia7d0rS37d13z0p5%2F21qsFuHWJ%2BDNtZUcnvvCqZ7hoiK5p%2FUDAz1kjCGIbd2f88CPKRo5j3T2fQKe29Pto3UlpjemRZDrCQEJA3nLExgdBYNkoS2d21nOKvurlMk87NN%2BrGQ5WGaSF9IvgPMaWszRUV5nEGr3pJ2kpfiyYWzJ6QCvZEKtImCM9OCgeBYTkm9lYKcysEFraRmW405rXbUeOyzHbcaaKF6gPrpJjl4EeRQ5d8qUyO4U%2BdoGi%2B8GiaCNwlYk%2BOpXnHv3R6AtEZpQPclUaw%2FPL81XKn8CdNULsvs%2BRCJkBqGLSPTEgCYAoKSd0%2BYYlki7wVl2EIUFA4cyM6WWYwRw95ZsGR7LD%2B4cRBUW1enLEmCe%2BBvmTT%2Bt8go4PaQkgWAbqxZoTaRxk2Jx43uqk1zxgxAKaVvTfZBVEUKOE4tPkPm8GvDvsXHjcG%2FB1U8Sto2WeL1HUwTvFcKwm3qWWHVgfnV%2B8Pv7qz0Wp%2FJRtIq%2FrhtmPg9LlKl3XqGZUkrrF%2Ff2T7%2FgOkdsF0aX9CG4NqMnW%2Fdg%2BobJUFqA7JRW45chFb%2Bx43OkWsRX%2BFbOvTNdjX8%2Bn24j%2F%2FIfzT1DzlMe8Yriwe1I4zNDwERMTJ7gbNRXX9cX60BxTsJIousbGS15at9sHngoKN0ecdDXqlcvixLgPUtARUdZRO7cX3HDiQaXSONtEAqB5PAQUv3%2F7XIDR%2FqyaYv0HYEqLbkMkobo%2FtnfGBDzji%2FXWmMoyP%2FsKnmYQHu7KL2Z0bDjsp7upRvbJQqgDas1nsiK3fRwJEetUxa%2Bk%2BllG6c%2BKcRRNdnAhiAXIaVyPcLoRz0RXcLwve1F0Bv6sMoX89v464Nr8FZ2xT4H%2FbR0ayVgjicTg5%2FXgTdpPJlZLz7dVAPMSrAf1Y3H9EsfLLb8k%2BYY6ZnMH1u02wnSqekULYuVYI8AGoK1MJYZ01Bk6%2F%2Bb3krv%2BeSki8NFuG0IQBBsqMVCbVdXgLg3Wyjlbf24xRJ3u9ix3hR11Fbz6YsLO1zivMjZU6pUX9MlCusoVx2p0Ex3L%2Fhm48WmRZlBJ79x3%2FFzRIcS1zZKWJFgzNI%2BV11dLvRUEwZU6EH8H585rx9eGFQ8Qjlkz0%2Bs1zujdtCxgG7ZK%2B7SICqK8MhNrbo%2BVnDBEwNhsUm5XFyA6kc%2BrlEsu0dfM0GYycjBZSA0%2BAbjOO7S6C4KnIcZYR8UTIMwmQwZ7xeXpZbX%2FOdbLXXvTgty2J973XeTV0jhHTm%2FfqJIl7eCfB80wFsuqDOFEEMCgLSxOoOK2oIQNABsb5Avxr3bZbYnJXgHMYLKpbB703C5KAdpAPv%2FLK5R0Pck%2BJH1aQ0D%2FbAfKD0Q9YRGgw09RsAvaDCXWpmrKpLu%2FoKD%2B55pfRolH2vIUM%2FAGOHI2tn9mcHrG8n4s9EWegFGMWhJVA7R48m3xWBv3cRPQHaNrz05Bs%2B6uJelmEkklhUNjuo8veCE65xeykKKOdMQZeVh9%2BwHgMnkBn%2BGrPimdbstwiYQL30dfCtkAXrrOrXDpuhEfCfHioa2gavO9m1k2v3WFTef0KbN9d%2Fd9f%2FBCZ6VCcbf4NrIVDCnRNPSTHNbYZHQ6oA6PYOkTOM0MIVxpbNQZD3s2pFxBR4zlStROBN1xLYsZFAohaJCx8iBPp%2B%2FKXtw2exAbg%2BK1yy2ambx8n2OijbaxDOyNTnDhq5KARY83fRliK8ctNjaKeb%2BpeCEUniBfJ%2Few%2FQdr3hpVILajm0iC6xKIDkrFI7C0abOD2sziTms6y1tetj%2Bb0LHx6NNcw1j1L1uQ07yIN2GxIpsS7Soi6YjVNTHanaUgMX1IbE81avhToQZai%2BH05FYfa9UOOVn7aseXc7SgTkuNt%2Fpz%2BLwrsOiGgRuwk%2Beuj14a9bNMedyS0tapGr0n7f9IkcceMgV%2BC02ZaegZyD1sLFrmkihS%2BIM%2BVr73NKiC5hhOo0wY92VNzqL%2FffNf6N%2Bcv9cH7KN0RmYjGwyEvcwmlFjsiln%2B%2F3VixIY06iDxJms%2B1SjmHemmtOyXs38mS8V5FU0AnCOVi5UzvWsPP4VPdETcL1gBqGDndX5MGzzC0ksR8EHVNng%2BYd9np7FgV%2FXUbIt1KmLkna%2Fivv3bJtehTDY%2FWzMGQm3RFgDojkBoTCA1Ou%2Fd6z5a25qAuJd4E5YFOXRYaIx6nZxTC%2FgNt1mxsRlW3fz04z%2B8niqxKNJB5%2BsAIMkCOTYRCfxpfM%2BUPb6hSVZcGOptkrxumeLPbh%2F7WV6RqdZTh6C%2BAEIbOnKcUMVI8vU5223c9rezakrFPut%2B2%2FCiwM9yN6Zt6Id%2FpZp%2B%2BTm223%2FR1%2B1XfBBLggvfjvi%2FFmkYO5hdOjrtUMD1fBvd%2BPUbuYMLCCXIPr4hKKIxblpUbgSmJbHfdvvrjwUeTqnxw%2BfOMjLRndDVdxupNVZ8Wytyih8oSN09Yej2u8ObU6216djR0DrYSIs3fi0pAknUPWLdVXfp9yiRNbk9%2F1OjdgTnWIpsAZZ8u57FSGV%2BeuxmOE8wsP0u4EjqEfe27Z49MDbw%3D%3D";     // raw __VIEWSTATE string
        public static string ValidationKeyHex = "9E4B1C57A2D3F0846B79C1D2E3F4A5B6C7D8E9FA0B1C2D3E4F5061728394A5B61A2B3C4D5E6F708192A3B4C5D6E7F8091A2B3C4D5E6F708192A3B4C5D6E7F809";
        public static string DecryptionKeyHex = "34C69D15ADD80DA4788E6E3D02694230CF8E9ADFDA2708EF43CAEF4C5BC73887";

        public static string Path = "/somepath/testaspx/test.aspx";
        public static string AppPath = "/testaspx/";

        const int IvLen = 16;     // AES block size
        const int HmacLen = 32;   // HMACSHA256

        public static object DecryptAndDeserialize()
        {
            // 1) URL-decode then Base64-decode
            string base64 = WebUtility.UrlDecode(RawViewStateUrlEncodedBase64);
            byte[] vsBytes = Convert.FromBase64String(base64);
            if (vsBytes.Length < IvLen + HmacLen)
                throw new InvalidDataException("Payload is too short");

            // 2) Split: tail is HMAC, head is IV+ciphertext 
            int macOffset = vsBytes.Length - HmacLen;
            byte[] data = vsBytes.Take(macOffset).ToArray(); // IV + ciphertext
            byte[] mac = vsBytes.Skip(macOffset).ToArray();

            // 3) Validate MAC (legacy style)
            byte[] validationKey = HexToBytes(ValidationKeyHex);
            var candidates = BuildLegacyModifierCandidates(Path, AppPath);

            byte[] chosenModifier = null;
            foreach (var mod in candidates)
            {
                if (VerifyLegacyMac(validationKey, data, mac, mod))
                {
                    chosenModifier = mod;
                    break;
                }
            }

            if (chosenModifier == null)
            {
                throw new CryptographicException("MAC verification failed for all legacy modifier candidates. Check path/appPath, casing, or keys.");
            }

            // 4) Decrypt AES-CBC PKCS7
            byte[] iv = data.Take(IvLen).ToArray();
            byte[] ciphertext = data.Skip(IvLen).ToArray();
            byte[] decKey = HexToBytes(DecryptionKeyHex);
            byte[] plaintext = AesCbcDecrypt(ciphertext, decKey, iv);

            // 5) Deserialize with LosFormatter
            var los = new LosFormatter();
            using (var ms = new MemoryStream(plaintext))
            {
                // LosFormatter often writes textual payload; stream is fine
                return los.Deserialize(ms);
            }
        }

        private static bool VerifyLegacyMac(byte[] validationKey, byte[] data, byte[] mac, byte[] modifier)
        {
            // Legacy path: HMAC(data) with modifier OR HMAC(modifier + data) depending on implementation.
            // We try both prepend and append to cover variants.
            using (var hmac = new HMACSHA256(validationKey))
            {
                // Prepend modifier
                byte[] pre = new byte[modifier.Length + data.Length];
                Buffer.BlockCopy(modifier, 0, pre, 0, modifier.Length);
                Buffer.BlockCopy(data, 0, pre, modifier.Length, data.Length);
                byte[] h1 = hmac.ComputeHash(pre);
                if (ConstantTimeEquals(mac, h1)) return true;

                // Append modifier
                byte[] post = new byte[data.Length + modifier.Length];
                Buffer.BlockCopy(data, 0, post, 0, data.Length);
                Buffer.BlockCopy(modifier, 0, post, data.Length, modifier.Length);
                byte[] h2 = hmac.ComputeHash(post);
                if (ConstantTimeEquals(mac, h2)) return true;

                // No modifier at all (some “legacy” uses raw data)
                byte[] h3 = hmac.ComputeHash(data);
                if (ConstantTimeEquals(mac, h3)) return true;
            }
            return false;
        }

        private static List<byte[]> BuildLegacyModifierCandidates(string path, string appPath)
        {
            // Normalize inputs
            string pLower = (path ?? "").Trim();
            string aLower = (appPath ?? "").Trim();

            // Common variants for legacy contexts:
            // - Virtual path lowercased vs as-is
            // - App path alone vs combined
            // - Encoding: UTF-8 vs UTF-16LE (Unicode)
            // - With or without trailing slash
            var paths = new HashSet<string>(StringComparer.Ordinal)
        {
            pLower,
            pLower.ToLowerInvariant(),
            aLower,
            aLower.ToLowerInvariant(),
            // Combined forms occasionally seen
            aLower + pLower,
            aLower.ToLowerInvariant() + pLower.ToLowerInvariant()
        };

            // Ensure trailing slash variants for app path
            if (!aLower.EndsWith("/")) paths.Add(aLower + "/");
            if (!aLower.ToLowerInvariant().EndsWith("/")) paths.Add(aLower.ToLowerInvariant() + "/");

            var encodings = new[] { Encoding.UTF8, Encoding.Unicode }; // UTF-16LE
            var modifiers = new List<byte[]>();

            foreach (var s in paths)
            {
                foreach (var enc in encodings)
                {
                    // Direct string bytes
                    modifiers.Add(enc.GetBytes(s));

                    // Also try a simple purpose prefix occasionally seen
                    modifiers.Add(enc.GetBytes("ViewState:" + s));
                }
            }

            // Deduplicate by content
            return modifiers
                .GroupBy(b => Convert.ToBase64String(b))
                .Select(g => g.First())
                .ToList();
        }

        private static byte[] AesCbcDecrypt(byte[] ciphertext, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;
                using (var d = aes.CreateDecryptor())
                {
                    return d.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                }
            }
        }

        private static byte[] HexToBytes(string hex)
        {
            hex = hex.Trim();
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);
            if (hex.Length % 2 != 0) throw new FormatException("Invalid hex length.");
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var output = VSDecrypt.VSDecryptor.DecryptAndDeserialize();
            Console.WriteLine(output.ToString());
        }
    }
}
