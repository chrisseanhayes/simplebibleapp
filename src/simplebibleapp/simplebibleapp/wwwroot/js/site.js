const EXCLUDED_STRONGS = new Set([
    'G3588', 'G2532', 'G1161', 'G1722', 'G1519', 'G1537', 'G4314', 'G3756', 'G3754', 'G3767', 'G1223', 'G2596', 'G3326', 'G1909', 'G575', 'G5259', 'G5228', 'G4862', 'G1437', 'G2443', 'G3739', 'G5613', 'G5620',
    'H853', 'H413', 'H5921', 'H3588', 'H5704', 'H8033', 'H5973', 'H310', 'H8478', 'H854', 'H1157', 'H1107'
]);

// Write your JavaScript code.
document.addEventListener('alpine:init', () => {
    Alpine.data('readApp', () => ({
        fullbible: false,
        defload: false,
        htmlItems: [], // List of active strongs reference numbers loaded (e.g. ['G3068'])
        wordRefs: [],  // Statistics of usage for the current ref
        bookAbbr: '',
        chapter: 1,
        activeRef: '',
        isExcluded: false,
        bookOccurrences: [],
        selectedUsageBook: '',

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

        init(bookAbbr, chapter) {
            this.bookAbbr = bookAbbr;
            this.chapter = chapter;

            // Setup event delegation on the #defs container for dynamic HTML elements
            const defsContainer = document.getElementById('defs');
            if (defsContainer) {
                defsContainer.addEventListener('click', (e) => {
                    const closeBtn = e.target.closest('.defclose');
                    if (closeBtn) {
                        const strongNum = closeBtn.getAttribute('data-strong');
                        this.closedef(strongNum, closeBtn);
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
            return items.some(item => this.htmlItems.includes(item));
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
        async getref(lemma) {
            if (!lemma) return;
            this.setSavedLemma(lemma);
            this.htmlItems = [];
            const defsContainer = document.getElementById('defs');
            if (defsContainer) defsContainer.innerHTML = '';
            
            const defs = lemma.split(' ');
            for (let i = 0; i < defs.length; i++) {
                const num = defs[i].split(':')[1];
                await this.loadRef(num);
            }
        },

        // Helper to load definition HTML and stats
        async loadRef(refnum) {
            if (this.htmlItems.includes(refnum)) return;
            this.defload = true;
            this.activeRef = refnum;
            this.selectedUsageBook = this.bookAbbr;
            this.bookOccurrences = [];

            if (EXCLUDED_STRONGS.has(refnum)) {
                this.isExcluded = true;
                this.wordRefs = [];
                this.defload = false;
                try {
                    const defResponse = await fetch('/Home/GetStrongRef/' + refnum);
                    if (defResponse.ok) {
                        const html = await defResponse.text();
                        this.htmlItems.push(refnum);
                        this.addRefHtml(html);
                        this.setSavedLemma(this.htmlItems.map(item => 'strong:' + item).join(' '));
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
                    this.htmlItems.push(refnum);
                    this.addRefHtml(html);
                    this.setSavedLemma(this.htmlItems.map(item => 'strong:' + item).join(' '));
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

        // Append definition HTML to container
        addRefHtml(html) {
            const defsContainer = document.getElementById('defs');
            if (!defsContainer) return;

            // Create a wrapper div for this definition
            const wrapper = document.createElement('div');
            wrapper.innerHTML = html;
            
            // Append the child nodes to avoid extra wrapper container styling issues
            while (wrapper.firstChild) {
                defsContainer.appendChild(wrapper.firstChild);
            }
        },

        // Close/remove a definition card
        closedef(strongsNumber, element) {
            this.htmlItems = this.htmlItems.filter(item => item !== strongsNumber);
            
            // Update selection persistence
            if (this.htmlItems.length === 0) {
                this.setSavedLemma(null);
                this.wordRefs = [];
            } else {
                this.setSavedLemma(this.htmlItems.map(item => 'strong:' + item).join(' '));
            }

            if (element) {
                // The structure is: <div class="greek-def"><i class="fa fa-times-circle defclose"></i> ...</div>
                // The parent elements are: defclose -> greek-def (parent) -> we want to remove the greek-def element (and its trailing <hr/> if present)
                const greekDefEl = element.closest('.greek-def');
                if (greekDefEl) {
                    // Check if there is an <hr> immediately after the greek-def, and remove it
                    const nextSibling = greekDefEl.nextSibling;
                    if (nextSibling && nextSibling.nodeName === 'HR') {
                        nextSibling.remove();
                    }
                    greekDefEl.remove();
                }
            }
        },

        // Click handler for "also see" links
        alsosee(lang, ref) {
            const prefix = (lang === 'HEBREW') ? 'H' : 'G';
            this.loadRef(prefix + ref);
        },

        // Decode HTML entities (needed for verse XML rendering in statistics list)
        decode(input) {
            if (!input) return '';
            if (/&amp;|&quot;|&#39;|'&lt;|&gt;/.test(input)) {
                const doc = new DOMParser().parseFromString(input, 'text/html');
                return doc.documentElement.textContent;
            }
            return input;
        }
    }));
});