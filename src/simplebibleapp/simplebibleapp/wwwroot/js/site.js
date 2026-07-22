const EXCLUDED_STRONGS = new Set([
    'G3588', 'G2532', 'G1161', 'G1722', 'G1519', 'G1537', 'G4314', 'G3756', 'G3754', 'G3767', 'G1223', 'G2596', 'G3326', 'G1909', 'G575', 'G5259', 'G5228', 'G4862', 'G1437', 'G2443', 'G3739', 'G5613', 'G5620',
    'H853', 'H413', 'H5921', 'H3588', 'H5704', 'H8033', 'H5973', 'H310', 'H8478', 'H854', 'H1157', 'H1107'
]);

// Write your JavaScript code.
document.addEventListener('alpine:init', () => {
    Alpine.data('readApp', () => ({
        fullbible: false,
        searchMenuVisible: false,
        defload: false,
        htmlItems: [], // List of active strongs reference objects e.g. [{ ref: 'G3068', html: '...' }]
        defActiveTab: null, // The currently active tab ref
        wordRefs: [],  // Statistics of usage for the current ref
        bookAbbr: '',
        chapter: 1,
        chapterHeading: '',
        activeRef: '',
        isExcluded: false,
        bookOccurrences: [],
        selectedUsageBook: '',
        
        // ── Book Search State ──────────────────────────────────────────────
        searchView: 'books', // 'books', 'loading', 'chapters', 'verses'
        selectedSearchBook: '',
        selectedSearchBookName: '',
        searchChapters: [],
        searchVerses: [],
        allBookVerses: null, // Map of chapter -> verses


        // ── Synonym / Linguistic Engine state ──────────────────────────────
        synonymData: null,       // AgyLinguisticPayloadDto from the API
        synonymLoading: false,   // spinner flag
        synonymError: null,      // error string or null
        synonymVisible: false,   // whether the synonym panel is shown
        synonymActiveRef: '',    // which strongs triggered the current analysis
        synonymConnectionId: null,
        synonymHub: null,
        synonymCache: {},        // Cache for synonym data keyed by strongs ref

        getSavedLemma() {
            try {
                return sessionStorage.getItem('selectedLemma');
            } catch (e) {
                return null;
            }
        },

        setSavedLemma(lemma) {
            try {
                if (lemma) {
                    sessionStorage.setItem('selectedLemma', lemma);
                } else {
                    sessionStorage.removeItem('selectedLemma');
                }
            } catch (e) {
                // Fail silently in private/incognito modes
            }
        },

        init(bookAbbr, chapter, chapterHeading) {
            this.bookAbbr = bookAbbr;
            this.chapter = chapter;
            this.chapterHeading = chapterHeading || '';

            // Setup event delegation on the .def-tab-content container for dynamic HTML elements
            const defsContainer = document.querySelector('.def-tab-content');
            if (defsContainer) {
                defsContainer.addEventListener('click', (e) => {
                    const closeBtn = e.target.closest('.defclose');
                    if (closeBtn) {
                        const strongNum = closeBtn.getAttribute('data-strong');
                        this.closedef(strongNum);
                    }
                    
                    const alsoSee = e.target.closest('.also-see');
                    if (alsoSee) {
                        const lang = alsoSee.getAttribute('data-lang');
                        const ref = alsoSee.getAttribute('data-ref');
                        this.alsosee(lang, ref);
                    }
                });
            }

            // Scroll the active chapter into view inside the minimap scroll container
            setTimeout(() => {
                const activeChNode = document.getElementById('minimap-ch-' + this.chapter);
                if (activeChNode) {
                    activeChNode.scrollIntoView({ block: 'center', behavior: 'smooth' });
                }
            }, 100);

            // Restore selection if saved
            const savedLemma = this.getSavedLemma();
            if (savedLemma) {
                setTimeout(() => {
                    this.getref(savedLemma);
                }, 50);
            }

            // Highlight selected verse on load and hash change
            window.addEventListener('hashchange', () => this.highlightTargetedVerse());
            setTimeout(() => {
                this.highlightTargetedVerse();
            }, 150);

            // Initialize SignalR connection for async synonym generation
            if (typeof signalR !== 'undefined') {
                this.synonymHub = new signalR.HubConnectionBuilder()
                    .withUrl("/linguisticHub")
                    .withAutomaticReconnect()
                    .build();

                this.synonymHub.on("ReceiveSynonyms", (data) => {
                    this.synonymData = data;
                    this.synonymLoading = false;
                    if (this.synonymActiveRef) {
                        this.synonymCache[this.synonymActiveRef] = data;
                    }
                });

                this.synonymHub.on("ReceiveSynonymsError", (err) => {
                    this.synonymError = err.error || "Unknown error from server.";
                    this.synonymLoading = false;
                });

                this.synonymHub.start().then(() => {
                    this.synonymConnectionId = this.synonymHub.connectionId;
                    console.log("SignalR connected with ID:", this.synonymConnectionId);
                }).catch(err => console.error("SignalR Connection Error: ", err));
            }
        },

        // Scan the DOM and highlight the targeted verse based on the URL anchor
        highlightTargetedVerse() {
            // Remove previous verse highlights
            document.querySelectorAll('.active-verse-highlight').forEach(el => {
                el.classList.remove('active-verse-highlight');
            });

            const hash = window.location.hash;
            if (!hash || !hash.startsWith('#vs-')) return;

            const targetId = hash.substring(1);
            const targetEl = document.getElementById(targetId);
            if (!targetEl) return;

            // Highlight the verse number itself
            targetEl.classList.add('active-verse-highlight');

            // Scroll the highlighted verse into view
            targetEl.scrollIntoView({ block: 'center', behavior: 'smooth' });

            // Highlight sibling nodes until we hit the next verse-number or end of verse (BR)
            let sibling = targetEl.nextSibling;
            while (sibling) {
                if (sibling.nodeType === Node.ELEMENT_NODE) {
                    if (sibling.classList.contains('verse-number') || sibling.nodeName === 'BR') {
                        break;
                    }
                    sibling.classList.add('active-verse-highlight');
                }
                sibling = sibling.nextSibling;
            }
        },

        // Check if a lemma is currently selected/loaded in the sidebar
        isSelectedLemma(lemmas) {
            if (!lemmas) return false;
            // lemmas is a space-separated list of refs, e.g. "strong:G3068 strong:G1234"
            const items = lemmas.split(' ').map(el => el.split(':')[1]);
            // Check if any of these lemmas are in the active loaded htmlItems list
            return items.some(item => this.htmlItems.some(h => h.ref === item));
        },

        // Check if a specific verse should be highlighted in the minimap
        isVerseHighlighted(chapter, verse) {
            if (!this.wordRefs || this.wordRefs.length === 0) return false;
            return this.wordRefs.some(ref => 
                ref.chapterAbbr.toLowerCase() === this.bookAbbr.toLowerCase() && 
                ref.chapterNumber === chapter && 
                ref.verseNumber === verse
            );
        },

        // Click handler to load references for a word
        async getref(lemma, event) {
            if (!lemma) return;
            this.htmlItems = [];
            this.defActiveTab = null;
            
            const defs = lemma.split(' ');
            let targetDefs = defs;
            
            // If it's a multi-lemma word phrase, filter out excluded words (like common articles) 
            // unless the user is holding Alt/Option key.
            const showAll = event && event.altKey;
            
            if (defs.length > 1 && !showAll) {
                const filteredDefs = defs.filter(d => !EXCLUDED_STRONGS.has(d.split(':')[1]));
                // If filtering removed all of them, just fallback to the original list
                if (filteredDefs.length > 0) {
                    targetDefs = filteredDefs;
                }
            }

            this.setSavedLemma(targetDefs.join(' '));

            for (let i = 0; i < targetDefs.length; i++) {
                const num = targetDefs[i].split(':')[1];
                await this.loadRef(num);
            }
        },

        // Helper to load definition HTML and stats
        async loadRef(refnum, skipHtmlPush = false) {
            if (!skipHtmlPush && this.htmlItems.some(h => h.ref === refnum)) {
                this.defActiveTab = refnum;
                this.updateSynonymState(refnum);
                return;
            }
            this.defload = true;
            this.activeRef = refnum;
            this.selectedUsageBook = this.bookAbbr;
            this.bookOccurrences = [];
            this.updateSynonymState(refnum);

            if (EXCLUDED_STRONGS.has(refnum)) {
                this.isExcluded = true;
                this.wordRefs = [];
                this.defload = false;
                try {
                    const defResponse = await fetch('/Home/GetStrongRef/' + refnum);
                    if (defResponse.ok) {
                        const html = await defResponse.text();
                        if (!skipHtmlPush) {
                            this.htmlItems.push({ ref: refnum, html: html });
                        }
                        if (!this.defActiveTab) this.defActiveTab = refnum;
                        this.setSavedLemma(this.htmlItems.map(item => 'strong:' + item.ref).join(' '));
                    }
                } catch (err) {
                    console.error('Error fetching reference definition:', err);
                }
                return;
            }

            this.isExcluded = false;

            try {
                // Fetch the HTML definition card
                const defResponse = await fetch('/Home/GetStrongRef/' + refnum);
                if (defResponse.ok) {
                    const html = await defResponse.text();
                    if (!skipHtmlPush) {
                        this.htmlItems.push({ ref: refnum, html: html });
                    }
                    if (!this.defActiveTab) this.defActiveTab = refnum;
                    this.setSavedLemma(this.htmlItems.map(item => 'strong:' + item.ref).join(' '));
                }

                // Fetch word usage references
                const refsResponse = await fetch('/Home/GetWordRefs/' + refnum + '?bookAbbr=' + encodeURIComponent(this.bookAbbr));
                if (refsResponse.ok) {
                    this.wordRefs = await refsResponse.json();
                }

                // Fetch aggregates
                const aggregatesResponse = await fetch('/Home/GetWordAggregates/' + refnum);
                if (aggregatesResponse.ok) {
                    this.bookOccurrences = await aggregatesResponse.json();
                }
            } catch (err) {
                console.error('Error fetching reference:', err);
            } finally {
                this.defload = false;
            }
        },

        async updateSynonymState(refnum) {
            if (this.synonymCache[refnum]) {
                this.synonymData = this.synonymCache[refnum];
                this.synonymActiveRef = refnum;
                return;
            }
            
            this.synonymData = null;
            this.synonymActiveRef = '';

            if (!this.chapterHeading) return;

            try {
                const params = new URLSearchParams({
                    strongs: refnum,
                    reference: this.chapterHeading
                });
                const resp = await fetch('/Home/CheckSynonymsCache?' + params.toString());
                if (resp.ok) {
                    const result = await resp.json();
                    if (result.cached && result.data) {
                        this.synonymCache[refnum] = result.data;
                        if (this.activeRef === refnum) {
                            this.synonymData = result.data;
                            this.synonymActiveRef = refnum;
                        }
                    }
                }
            } catch (err) {
                console.error('Error checking synonym cache:', err);
            }
        },

        async selectUsageBook(bookAbbr) {
            if (!this.activeRef || this.isExcluded) return;
            this.selectedUsageBook = bookAbbr;
            this.defload = true;
            try {
                const refsResponse = await fetch('/Home/GetWordRefs/' + this.activeRef + '?bookAbbr=' + encodeURIComponent(bookAbbr));
                if (refsResponse.ok) {
                    this.wordRefs = await refsResponse.json();
                }
            } catch (err) {
                console.error('Error switching word usage book:', err);
            } finally {
                this.defload = false;
            }
        },

        // Close/remove a definition card
        closedef(strongsNumber) {
            this.htmlItems = this.htmlItems.filter(item => item.ref !== strongsNumber);
            
            if (this.defActiveTab === strongsNumber) {
                this.defActiveTab = this.htmlItems.length > 0 ? this.htmlItems[0].ref : null;
            }

            // Update selection persistence
            if (this.htmlItems.length === 0) {
                this.setSavedLemma(null);
                this.wordRefs = [];
                this.bookOccurrences = [];
                this.activeRef = '';
                this.synonymData = null;
                this.synonymActiveRef = '';
            } else {
                this.setSavedLemma(this.htmlItems.map(item => 'strong:' + item.ref).join(' '));
                if (this.defActiveTab) {
                    this.loadRef(this.defActiveTab, true);
                }
            }
        },

        // Sidebar Book Search methods
        async selectSearchBook(bookAbbr, bookName) {
            this.selectedSearchBook = bookAbbr;
            this.selectedSearchBookName = bookName;
            this.searchView = 'loading';
            try {
                const res = await fetch('/Home/GetBookVerses?id=' + encodeURIComponent(bookAbbr));
                const verses = await res.json();
                
                const chaptersMap = new Map();
                verses.forEach(v => {
                    const ch = v.chapter || v.Chapter;
                    if (!chaptersMap.has(ch)) chaptersMap.set(ch, []);
                    chaptersMap.get(ch).push(v);
                });
                
                this.searchChapters = Array.from(chaptersMap.keys()).sort((a,b) => a-b);
                this.allBookVerses = chaptersMap;
                this.searchView = 'chapters';
            } catch (err) {
                console.error('Error fetching book chapters:', err);
                this.searchView = 'books';
            }
        },
        
        selectSearchChapter(chap) {
            this.searchVerses = this.allBookVerses.get(chap);
            this.searchView = 'verses';
        },
        
        resetSearch() {
            this.searchView = 'books';
            this.selectedSearchBook = '';
            this.selectedSearchBookName = '';
        },

        // Click handler for "also see" links
        alsosee(lang, ref) {
            const prefix = (lang === 'HEBREW') ? 'H' : 'G';
            this.loadRef(prefix + ref);
        },

        // Decode HTML entities (needed for verse XML rendering in statistics list)
        decode(input) {
            if (!input) return '';
            if (/&amp;|&quot;|&#39;|&lt;|&gt;/.test(input)) {
                const txt = document.createElement("textarea");
                txt.innerHTML = input;
                return txt.value;
            }
            return input;
        },

        // ── Synonym / Linguistic Engine ────────────────────────────────────

        /**
         * Load a synonym analysis for the currently-selected Strong's number.
         * @param {string} strongs  e.g. 'G3056'
         * @param {string} reference  e.g. 'John 1:1'
         * @param {string} lemma  optional original-language lemma text
         * @param {string} language  'Greek' or 'Hebrew'
         */
        async loadSynonyms(strongs, reference, lemma, language) {
            if (!strongs || !reference) return;
            if (this.synonymLoading) return; // debounce

            this.synonymActiveRef = strongs;
            this.synonymLoading = true;
            this.synonymError = null;
            this.synonymData = null;
            this.synonymVisible = true;

            try {
                const params = new URLSearchParams({
                    strongs,
                    reference,
                    lemma: lemma || '',
                    language: language || 'Greek',
                    connectionId: this.synonymConnectionId || ''
                });
                const resp = await fetch('/Home/GetSynonyms?' + params.toString());
                if (!resp.ok) {
                    const errBody = await resp.json().catch(() => ({ error: resp.statusText }));
                    this.synonymError = errBody?.error || `Error ${resp.status}`;
                    this.synonymLoading = false;
                    return;
                }
                // synonymData and synonymLoading will be updated by SignalR callback
            } catch (err) {
                this.synonymError = 'Network error — could not reach the synonym engine.';
                this.synonymLoading = false;
                console.error('loadSynonyms error:', err);
            }
        },

        /** Close/dismiss the synonym panel */
        closeSynonyms() {
            this.synonymVisible = false;
            this.synonymData = null;
            this.synonymError = null;
            this.synonymActiveRef = '';
        },

        /** Confidence badge colour (green → amber → red) */
        confidenceClass(score) {
            if (score >= 0.80) return 'conf-high';
            if (score >= 0.55) return 'conf-mid';
            return 'conf-low';
        },

        /** Relationship label friendly display */
        relationshipLabel(rel) {
            const map = {
                'direct_synonym': 'Direct Synonym',
                'lxx_translation_equivalent': 'LXX Equivalent',
                'semantic_neighbor': 'Semantic Neighbor',
                'antonym': 'Antonym'
            };
            return map[rel] || rel;
        }
    }));
});