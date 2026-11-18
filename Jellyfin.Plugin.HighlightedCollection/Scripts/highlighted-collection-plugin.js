/**
 * heavily modified from https://gist.github.com/malte9799/691a52da31f703d176d4f913c5de5fe4
 * should swap to https://github.com/jellyfin/jellyfin-sdk-typescript
 */
(function () {
    "use strict";

    const CONFIG = {
        Collection: '##Collection##',
        Title: '##Title##',
        SortOrder: '##SortOrder##',
    };

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
    function fetchCollectionItems() {
        const api = getApiClient();

        let options = {
            ParentId: CONFIG.Collection,
            Limit: 16
        };

        if (CONFIG.SortOrder == "Name") {
            options.SortBy = "SortName";
            options.SortOrder = "Ascending";
        }

        if (CONFIG.SortOrder == "DateAddedToCollection") {
            options.Fields = "Tags";
        } else {
            options.Fields = "PremiereDate";
        }

        return api
            .getItems(api.getCurrentUserId(), options)
            .then((result) => {
                if (CONFIG.SortOrder == "DateAddedToCollection") {
                    result.Items.sort(sortByDate);
                }
                result.Items.slice(0, 16);
                return result;
            })
            .catch((e) => {
                err("getItems failed for", CONFIG.Collection, e);
                throw e;
            });
    }

    function sortByDate(a, b) {
        let aTags = a.Tags.filter(tag => tag.startsWith("highlighted:"));
        let bTags = b.Tags.filter(tag => tag.startsWith("highlighted:"));
        let aTagCount = aTags.length;
        let bTagCount = bTags.length;
        if (aTagCount == 0 && bTagCount == 0) {
            return 0; // keep original sorting
        }
        if (aTagCount == 0) {
            return 1; // put a after b
        }
        if (bTagCount == 0) {
            return -1; // put a before b
        }

        let aTag = aTags[0];
        let bTag = bTags[0];
        if (aTag < bTag) {
            return -1; // put a before b
        }
        if (aTag > bTag) {
            return 1; // put b after a
        }
        return 0; // keep original sorting
    }

    /** Collection display. */
    function buildPoster(item, index, dateAdded) {
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
        if (dateAdded) {
            let tag = item.Tags.filter(tag => tag.startsWith("highlighted:"));
            if (tag.length > 0) cardTextSecond.append($('<bdi>' + tag[0].substring(12) + '</bdi>'));
        } else if (item.PremiereDate) {
            cardTextSecond.append($('<bdi>' + item.PremiereDate.substring(0, 4) + '</bdi>'));
        }
        cardBox.append(cardTextSecond);

        container.append(cardBox);
        return container;
    }

    function buildSection(items) {
        if (items.TotalRecordCount > 0) {
            const api = getApiClient();
            let highlightedOuter = $('<div class="verticalSection emby-scroller-container" id="highlighted-collection"></div>');

            let highlighted = $('<div class="verticalSection emby-scroller-container"></div>');
            highlighted.append(
                $('<div class="sectionTitleContainer sectionTitleContainer-cards padded-left"><a is="emby-linkbutton" href="#/details?id=' + CONFIG.Collection + '&amp;serverId=' + api.serverId() + '" class="more button-flat button-flat-mini sectionTitleTextButton emby-button"><h2 class="sectionTitle sectionTitle-cards">' + CONFIG.Title + '</h2><span class="material-icons chevron_right" aria-hidden="true"></span></a></div>')
            );
            let detailsOuter = $('<div is="emby-scroller" class="padded-top-focusscale padded-bottom-focusscale emby-scroller" data-centerfocus="true" data-scroll-mode-x="custom"></div>');
            let details = $('<div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x animatedScrollX" style="white-space: nowrap; will-change: transform; transition: transform 270ms ease-out; transform: translateX(0px);"></div>');

            for (let [index, item] of items.Items.entries()) {
                details.append(buildPoster(item, index, CONFIG.SortOrder == "DateAddedToCollection"));
            }

            detailsOuter.append(details);
            highlighted.append(detailsOuter);

            highlightedOuter.append(highlighted);

            // Might take a split second for section1 to show up.
            setTimeout(() => $(".homeSectionsContainer").prepend(highlightedOuter), 100);
        }
    }

    function display() {
        if (window.location.href.endsWith("/web/#/home") && !$('#leaving-soon')[0]) {
            const api = getApiClient();
            if (!api) {
                warn("ApiClient not ready");
                return Promise.reject(new Error("ApiClient not ready"));
            }
            fetchCollectionItems().then((result) => buildSection(result));
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
