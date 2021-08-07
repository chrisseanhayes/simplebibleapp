// Write your JavaScript code.
angular.module("readApp", ["ngSanitize","ngResource"])
    .factory("strongFactory", ["$http", function ($http) {
        var fact = {};
        var uribase = '/Home/';
        fact.getStrongRef = function (lemma) {
            return $http.get(uribase + 'GetStrongRef/' + lemma);
        };
        return fact;
    }])
    .factory("wordRefFactory", ["$http", function ($http) {
        var fact = {};
        var uribase = '/Home/';
        fact.getRefs = function (lemma) {
            return $http.get(uribase + 'GetWordRefs/' + lemma);
        };
        return fact;
    }])
    .filter("trust", ['$sce', function ($sce) {
        return function (htmlCode) {
            return $sce.trustAsHtml(htmlCode);
        };
    }])
    .factory("heartFactory", ["$http", function($http) {
        var fact = {};
        var uribase = '/Home/';
        fact.setHeart = function (sid, selected) {
            var parts = sid.split('.');
            return $http.post(uribase +
                'HeartVerse?bookAbbr=' +
                parts[0] +
                '&chapter=' +
                parts[1] +
                '&verse=' +
                parts[2] +
                '&selected=' +
                selected);
        };
        fact.getChapterHearts = function (bookAbbr, chapter) {
            return $http.get(uribase +
                'GetChapterHearts?bookAbbr=' +
                bookAbbr +
                '&chapter=' +
                chapter);
        };
        return fact;
    }])
    .controller("readController",
        [
            "$scope", "strongFactory", "wordRefFactory", "heartFactory", "$compile", function ($scope, strongFactory, wordRefFactory, heartFactory, $compile) {
                $scope.heartedVerses = {};
                $scope.htmlItems = [];
                $scope.defload = false;
                $scope.isSelectedLemma = function (lemmas) {
                    var items = lemmas.split(' ').map((el, i) => {
                        return el.split(':')[1];
                    });
                    return _.intersection($scope.htmlItems, items).length > 0;
                };
                var addref = function (html, id) {
                    if (_.contains($scope.htmlItems, id)) { return; }
                    $scope.htmlItems.push(id);
                    var el = document.getElementById("defs");
                    angular.element(el).append($compile(html)($scope));
                };
                $scope.closedef = function (evt,strongsNumber) {
                    $scope.htmlItems = _.without($scope.htmlItems, strongsNumber);
                    $(evt.target).parent().remove();
                };
                $scope.alsosee = function (lang, ref) {
                    var pref = 'G';
                    if (lang === 'HEBREW') {
                        pref = 'H';
                    }
                    loadRef(pref + ref);
                };
                $scope.decode = function(input) {
                    if (/&amp;|&quot;|&#39;|'&lt;|&gt;/.test(input)) {
                        var doc = new DOMParser().parseFromString(input, "text/html");
                        return doc.documentElement.textContent;
                    }
                    return input;
                }
                function loadRef(refnum) {
                    if (_.contains($scope.htmlItems, refnum)) { return; }
                    $scope.defload = true;
                    strongFactory.getStrongRef(refnum).then(
                        function (rsp) {
                            $scope.defload = false;
                            addref(rsp.data, refnum);
                        }, function (err) {
                            $scope.defload = false;
                    });
                    wordRefFactory.getRefs(refnum).then(
                        function (rsp) {
                            $scope.wordRefs = rsp.data;
                        }, function (err) {
                            $scope.wordRefs = [];
                        }
                    );
                }
                $scope.getref = function (lemma) {
                    $scope.selectedLemma = lemma;
                    $scope.htmlItems = [];
                    var defs = lemma.split(' ');
                    $('#defs').empty();
                    for (var i = 0; i < defs.length; i++) {
                        $scope.defload = true;
                        var num = defs[i].split(':')[1];
                        loadRef(num);
                    }
                };
                $scope.heartClick = function (sid) {
                    var hearted = $scope.heartedVerses[sid];
                    console.log("heart clicked:" + sid + " checked:" + hearted);
                    heartFactory.setHeart(sid, hearted)
                        .then(function(rsp) {

                            },
                            function(err) {
                                $scope.heartedVerses[sid] = !hearted;
                            });
                };
                $scope.init = function(bookAbbr, chapter) {
                    heartFactory.getChapterHearts(bookAbbr, chapter)
                        .then(function(rsp) {
                            console.log(rsp);
                            for (var i = 0; i < rsp.data.length; i++) {
                                $scope.heartedVerses[rsp.data[i]] = true;
                            }
                        }, function(err) {

                        });
                };
            }
        ]);