using Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace Services
{
    public class ScraperService
    {
        private readonly ChromeDriver _driver;
        private readonly MediaRepository _mediaRepository;

        public ScraperService(MediaRepository mediaRepository)
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--headless"); 
            _driver = new ChromeDriver(options);
            _mediaRepository = mediaRepository;
        }

        public void ScrapeAndSaveData(string url)
        {
            _driver.Navigate().GoToUrl(url);
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));

            HashSet<int> seenHashCodes = new HashSet<int>();
            int maxScrolls = 15, scrollCount = 0;

            while (scrollCount < maxScrolls)
            {
                var wrapperContainers = _driver.FindElements(By.CssSelector("div.tray-container"));

                foreach (var container in wrapperContainers)
                {
                    int id = container.GetHashCode();
                    if (seenHashCodes.Contains(id)) continue;
                    seenHashCodes.Add(id);

                    var nextBtn = TryGetElement(container, ".swiper-button-next");
                    bool hasNext = nextBtn != null;

                    while (true)
                    {
                        int visited = VisitCards(_driver, container, wait, 5);
                        if (visited == 0) break;

                        if (hasNext && nextBtn != null && !nextBtn.GetAttribute("class").Contains("swiper-button-disabled"))
                        {
                            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", nextBtn);
                            Thread.Sleep(2000);
                            nextBtn = TryGetElement(container, ".swiper-button-next");
                        }
                        else break;
                    }
                }

                scrollCount++;
                ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollBy(0, 1500);");
                Thread.Sleep(3000);
            }
            _driver.Quit();
        }

        private int VisitCards(IWebDriver driver, IWebElement container, WebDriverWait wait, int limit)
        {
            int visited = 0;
            var cards = container.FindElements(By.CssSelector("div.swiper-slide"));

            foreach (var card in cards)
            {
                if (visited >= limit) break;

                var linkEl = card.FindElements(By.CssSelector("div[data-testid='action']")).FirstOrDefault();
                if (linkEl == null) continue;

                string rawLabel = linkEl.GetAttribute("aria-label") ?? "";
                string title = rawLabel.Split(',')[0].Trim();
                string type = rawLabel.Split(',')[1].Trim();

                string href = linkEl.GetAttribute("href");
                Console.WriteLine($"Visiting: {title}");
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", linkEl);
                Thread.Sleep(500);
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", linkEl);

                try
                {
                    wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").ToString() == "complete");
                    Thread.Sleep(2000);
                    Console.WriteLine($"Visited: {driver.Url}");
                    GetDetailPageInfo(driver, wait, type, title);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Detail page load error: {ex.Message}");
                }

                driver.Navigate().Back();
                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").ToString() == "complete");
                Thread.Sleep(2000);

                visited++;
            }

            return visited;
        }
        private IWebElement TryGetElement(IWebElement parent, string selector)
        {
            return parent.FindElements(By.CssSelector(selector)).FirstOrDefault();
        }

        void GetDetailPageInfo(IWebDriver driver, WebDriverWait wait, string type, string title)
        {
            try
            {
                wait.Until(d =>
                {
                    var shimmerGone = !d.FindElements(By.CssSelector("#shimmerContainer [data-testid='skeleton']")).Any();
                    var descriptionVisible = d.FindElements(By.CssSelector("div._1SQXlCXyLucI91Ny_sWM9q p")).Any();
                    return shimmerGone || descriptionVisible;
                });

                Thread.Sleep(1000);

                var script = @"let el = document.querySelector('div._1SQXlCXyLucI91Ny_sWM9q p');
                               return el ? el.innerText : '';";
                string description = ((IJavaScriptExecutor)driver).ExecuteScript(script)?.ToString()?.Trim();

                var genreScript = @"let el = document.querySelector('div[data-testid=""tagFlipperEnriched""] span');
                                    return el ? el.innerText : '';";
                string genre = ((IJavaScriptExecutor)driver).ExecuteScript(genreScript)?.ToString()?.Trim();

                var episodes = driver.FindElements(By.CssSelector("li[data-testid='episode-card']"));

                List<Season> seasonDatas = new List<Season>();
                List<Episode> eps = new List<Episode>();

                foreach (var episode in episodes)
                {
                    var ep_title = episode.FindElement(By.CssSelector("h3")).Text;
                    var imageUrl = episode.FindElement(By.CssSelector("img")).GetAttribute("src");
                    var seasonEpisode = episode.FindElement(By.XPath(".//span[contains(text(),'S1 E')]")).Text;
                    var releaseDate = episode.FindElements(By.XPath(".//span[contains(@class, 'LABEL_CAPTION1_SEMIBOLD')]"))[1].Text;
                    var duration = episode.FindElements(By.XPath(".//span[contains(@class, 'LABEL_CAPTION1_SEMIBOLD')]"))[2].Text;
                    var description1 = episode.FindElement(By.CssSelector("p")).Text;
                    var link = episode.FindElement(By.CssSelector("a")).GetAttribute("href");

                    eps.Add(new Episode()
                    {
                        Title = ep_title,
                        Description = description1,
                        Duration = duration,
                        Link = link,
                        ImageUrl = imageUrl
                    });

                    Console.WriteLine($"{seasonEpisode}: {ep_title} ({duration}) on {releaseDate}");
                    Console.WriteLine($"Link: {link}");
                    Console.WriteLine($"Image: {imageUrl}");
                    Console.WriteLine($"Description: {description1}");
                }

                seasonDatas.Add(new Season() { SeasonTitle = "Season", Episodes = eps });
                var dataToBeInserted = new MediaData() { Description = description, Title = title, Genre = genre, Seasons = seasonDatas };

                _mediaRepository.SaveMediaData(dataToBeInserted);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] GetDetailPageInfo: {ex.Message}");
            }
        }
    }
}
