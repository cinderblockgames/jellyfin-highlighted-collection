/**
 * heavily modified from https://gist.github.com/malte9799/691a52da31f703d176d4f913c5de5fe4
 * should swap to https://github.com/jellyfin/jellyfin-sdk-typescript
 */
(function () {
    "use strict";

    // Real problems only — no verbose tracing.
    const warn = (...args) => console.warn("[highlighted-collection]", ...args);
    const err = (...args) => console.error("[highlighted-collection]", ...args);

    // ---- Jellyfin API helpers ---------------------------------------------
    function getApiClient() {
        return window.ApiClient || null;
    }

    function getPosterUrl(item, api) {
        const opts = { type: "Primary", maxHeight: 300, quality: 90, tag: item.ImageTags && item.ImageTags.Primary };
        return typeof api.getScaledImageUrl === "function"
            ? api.getScaledImageUrl(item.Id, opts)
            : api.getImageUrl(item.Id, opts);
    }

    /** Fetches a collection's first 16 child items, sorted by highlighted: tag (if DateAddedToCollection is the SortOrder). */
    function fetchCollectionItems(data) {
        const api = getApiClient();

        let options = {
            ParentId: data.Config.Collection,
            Limit: 16
        };

        if (data.Config.SortOrder == "Name") {
            options.SortBy = "SortName";
            options.SortOrder = "Ascending";
        }

        if (data.Config.SortOrder == "DateAddedToCollection") {
            options.Ids = Object.keys(data.AddedDates);
        } else {
            options.Fields = "PremiereDate";
        }

        return api
            .getItems(api.getCurrentUserId(), options)
            .then((result) => {
                if (data.Config.SortOrder == "DateAddedToCollection") {
                    result.Items.forEach(item => item.DateAddedToCollection = data.AddedDates[item.Id]);
                    result.Items.sort(sortByDate);
                }
                result.Items.slice(0, 16);
                return result;
            })
            .catch((e) => {
                err("getItems failed for", data.config.Collection, e);
                throw e;
            });
    }

    function sortByDate(a, b) {
        if (a.DateAddedToCollection < b.DateAddedToCollection) {
            return -1; // put a before b
        }
        if (a.DateAddedToCollection > b.DateAddedToCollection) {
            return 1; // put b after a
        }
        return 0; // keep original sorting
    }

    /** Collection display. */
    function buildPoster(item, index, showDateAdded) {
        const api = getApiClient();
        let container = $('<div data-index="' + index + '" data-isfolder="false" data-serverid="' + item.ServerId + '" data-id="' + item.Id + '" data-type="' + item.Type + '" data-mediatype="' + item.MediaType + '" data-path="" data-context="home" data-prefix="RES" class="card overflowPortraitCard card-hoverable card-withuserdata"></div>');
        let cardBox = $('<div class="cardBox cardBox-bottompadded"></div>');

        let link = '#/details?id=' + item.Id + '&amp;serverId=' + item.ServerId;

        let cardScalable = $('<div class="cardScalable"></div>');
        cardScalable.append($('<div class="cardPadder cardPadder-overflowPortrait lazy-hidden-children"><span class="cardImageIcon material-icons movie" aria-hidden="true"></span></div>'));
        cardScalable.append($('<canvas aria-hidden="true" width="20" height="20" class="blurhash-canvas lazy-hidden"></canvas>'));
        cardScalable.append($('<a href="' + link + '" data-action="link" class="cardImageContainer coveredImage cardContent itemAction lazy blurhashed lazy-image-fadein-fast" aria-label="' + item.Name + '" role="img" style="background-image: url(&quot;' + api.getScaledImageUrl(item.Id, {type: "primary", fillHeight: 326, fillWidth: 217}) + '&quot;);"></a>'));
        cardScalable.append($('<div class="cardOverlayContainer itemAction" data-action="link"><button is="paper-icon-button-light" class="cardOverlayButton cardOverlayButton-hover itemAction paper-icon-button-light cardOverlayFab-primary" data-action="resume" title="Play"><span class="material-icons cardOverlayButtonIcon cardOverlayButtonIcon-hover play_arrow" aria-hidden="true"></span></button><'));
        cardBox.append(cardScalable);

        let cardTextFirst = $('<div class="cardText cardTextCentered cardText-first"></div>');
        cardTextFirst.append($('<bdi><a href="' + link + '" data-id="'+ item.Id +'" data-serverid="' + item.ServerId + '" data-type="' + item.Type + '" data-mediatype="' + item.MediaType + '" data-channelid="' + item.ChannelId + '" data-isfolder="' + item.IsFolder + '" class="itemAction textActionButton" title="' + item.Name + '" data-action="link">' + item.Name + '</a></bdi>'));
        cardBox.append(cardTextFirst);

        let cardTextSecond = $('<div class="cardText cardTextCentered cardText-secondary"></div>');
        if (showDateAdded) {
            if (item.DateAddedToCollection) cardTextSecond.append($('<bdi>' + item.DateAddedToCollection + '</bdi>'));
        } else if (item.PremiereDate) {
            cardTextSecond.append($('<bdi>' + item.PremiereDate.substring(0, 4) + '</bdi>'));
        }
        cardBox.append(cardTextSecond);

        container.append(cardBox);
        return container;
    }

    function buildSection(items, data) {
        if (items && items.TotalRecordCount > 0) {
            const api = getApiClient();
            let highlightedOuter = $('<div class="verticalSection emby-scroller-container" id="highlighted-collection"></div>');

            let highlighted = $('<div class="verticalSection emby-scroller-container"></div>');
            highlighted.append(
                $('<div class="sectionTitleContainer sectionTitleContainer-cards padded-left"><a is="emby-linkbutton" href="#/details?id=' + data.Config.Collection + '&amp;serverId=' + api.serverId() + '" class="more button-flat button-flat-mini sectionTitleTextButton emby-button"><h2 class="sectionTitle sectionTitle-cards">' + data.Config.Title + '</h2><span class="material-icons chevron_right" aria-hidden="true"></span></a></div>')
            );
            let detailsOuter = $('<div is="emby-scroller" class="padded-top-focusscale padded-bottom-focusscale emby-scroller" data-centerfocus="true" data-scroll-mode-x="custom"></div>');
            let details = $('<div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x animatedScrollX" style="white-space: nowrap; will-change: transform; transition: transform 270ms ease-out; transform: translateX(0px);"></div>');

            for (let [index, item] of items.Items.entries()) {
                details.append(buildPoster(item, index, data.Config.SortOrder == "DateAddedToCollection"));
            }

            detailsOuter.append(details);
            highlighted.append(detailsOuter);

            highlightedOuter.append(highlighted);

            // Might take a split second for container to show up.
            setTimeout(() => addSection(highlightedOuter), 100);
        }
    }

    function addSection(section) {
        if ($(".homeSectionsContainer > div:first-child h2").text() == "My Media") {
            $(".homeSectionsContainer > div:first-child").after(section);
        } else {
            $(".homeSectionsContainer").prepend(section);
        }
    }

    function display() {
        if (window.location.href.endsWith("/web/#/home") && !$('#highlighted-collection')[0]) {
            const api = getApiClient();
            if (!api) {
                warn("ApiClient not ready");
                return Promise.reject(new Error("ApiClient not ready"));
            }
            api.getJSON(api.getUrl('/HighlightedCollection/CollectionData?count=16'))
                .then(data => {
                    if (!data) return;
                    fetchCollectionItems(data).then(items => buildSection(items, data));
                });
        }
    }

    // Initial load.
    display();

    // Navigation.
    function watchHistoryEvents() {
        const { pushState, replaceState } = window.history;

        window.history.pushState = function (...args) {
            pushState.apply(window.history, args);
            window.dispatchEvent(new Event('pushState'));
        };

        window.history.replaceState = function (...args) {
            replaceState.apply(window.history, args);
            window.dispatchEvent(new Event('replaceState'));
        };

        window.addEventListener('popstate', display);
        window.addEventListener('replaceState', display);
        window.addEventListener('pushState', display);
    }
    watchHistoryEvents();
})();
