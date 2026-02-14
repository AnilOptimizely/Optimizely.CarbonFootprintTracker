/**
 * Carbon Tracker - Optimizely CMS Panel JavaScript
 * Handles report loading, rendering, CMS context changes, and export.
 */
(function () {
    'use strict';

    var currentContentLink = null;
    var currentReport = null;
    var apiBase = '/api/carbon-tracker';

    // --- Initialization ---
    document.addEventListener('DOMContentLoaded', function () {
        // Read contentLink from query string or ViewBag injection
        var params = new URLSearchParams(window.location.search);
        currentContentLink = params.get('contentLink') || window.__carbonTracker_contentLink || null;

        if (currentContentLink) {
            loadReport(currentContentLink);
        } else {
            showNotPublished();
        }

        // Wire up buttons
        var reanalyzeBtn = document.getElementById('btnReanalyze');
        if (reanalyzeBtn) reanalyzeBtn.addEventListener('click', reanalyze);

        var exportBtn = document.getElementById('btnExport');
        if (exportBtn) exportBtn.addEventListener('click', exportReport);
    });

    // --- CMS Context Listeners ---
    // Listen for Optimizely CMS context changes (content selection)
    window.addEventListener('message', function (event) {
        if (event.data && event.data.contentLink) {
            currentContentLink = event.data.contentLink;
            loadReport(currentContentLink);
        }
    });

    // Support the epi beta/context change pattern
    if (window.epi) {
        if (window.epi.subscribe) {
            window.epi.subscribe('beta/contentSaved', function () {
                if (currentContentLink) loadReport(currentContentLink);
            });
            window.epi.subscribe('beta/navigate', function (message) {
                if (message && message.contentLink) {
                    currentContentLink = message.contentLink;
                    loadReport(currentContentLink);
                }
            });
        }
    }

    // --- Data Loading ---
    function loadReport(contentLink) {
        showLoading();

        // Try cached report first
        fetch(apiBase + '/report/' + encodeURIComponent(contentLink))
            .then(function (res) {
                if (res.ok) return res.json();
                if (res.status === 404) return null;
                throw new Error('HTTP ' + res.status);
            })
            .then(function (report) {
                if (report) {
                    currentReport = report;
                    displayReport(report);
                    loadHistory(contentLink);
                } else {
                    // No cached report — trigger analysis
                    triggerAnalysis(contentLink);
                }
            })
            .catch(function (err) {
                showError(err.message);
            });
    }

    function triggerAnalysis(contentLink) {
        showLoading();

        fetch(apiBase + '/analyze/' + encodeURIComponent(contentLink))
            .then(function (res) {
                if (!res.ok) throw new Error('HTTP ' + res.status);
                return res.json();
            })
            .then(function (report) {
                currentReport = report;
                displayReport(report);
                loadHistory(contentLink);
            })
            .catch(function (err) {
                showError(err.message);
            });
    }

    function loadHistory(contentLink) {
        fetch(apiBase + '/history/' + encodeURIComponent(contentLink) + '?days=30')
            .then(function (res) { return res.ok ? res.json() : []; })
            .then(function (history) { displaySparkline(history); })
            .catch(function () { /* silently ignore history failures */ });
    }

    function reanalyze() {
        if (currentContentLink) triggerAnalysis(currentContentLink);
    }

    // --- UI State Management ---
    function showLoading() {
        toggleSection('loading', true);
        toggleSection('error', false);
        toggleSection('report', false);
        toggleSection('notPublished', false);
    }

    function showError(message) {
        toggleSection('loading', false);
        toggleSection('report', false);
        toggleSection('notPublished', false);
        var el = document.getElementById('error');
        if (el) {
            el.innerHTML = '<div>Error: ' + escapeHtml(message) + '</div>' +
                '<button class="error-retry" onclick="window.__carbonTracker_retry()">Retry</button>';
            el.style.display = 'block';
        }
    }

    function showNotPublished() {
        toggleSection('loading', false);
        toggleSection('error', false);
        toggleSection('report', false);
        var el = document.getElementById('notPublished');
        if (el) el.style.display = 'block';
    }

    function toggleSection(id, show) {
        var el = document.getElementById(id);
        if (el) el.style.display = show ? 'block' : 'none';
    }

    // Expose retry for inline onclick
    window.__carbonTracker_retry = function () {
        if (currentContentLink) loadReport(currentContentLink);
    };

    // --- Report Rendering ---
    function displayReport(report) {
        toggleSection('loading', false);
        toggleSection('error', false);
        toggleSection('notPublished', false);
        toggleSection('report', true);

        displayScoreBadge(report);
        displayAssetBreakdown(report.assets || []);
        displaySuggestions(report.suggestions || []);
    }

    function displayScoreBadge(report) {
        var score = report.score;
        var scoreStr = typeof score === 'number' ? ['A','B','C','D','F'][score] || score : score;
        var co2 = (report.estimatedCO2Grams || 0).toFixed(2);

        var circle = document.getElementById('scoreCircle');
        if (circle) circle.className = 'score-circle score-' + scoreStr;

        var letter = document.getElementById('scoreLetter');
        if (letter) letter.textContent = scoreStr;

        var co2El = document.getElementById('scoreCO2');
        if (co2El) co2El.textContent = co2 + 'g CO\u2082';

        // Compute comparison based on HTTP Archive median (~2.4MB page ≈ 0.87g)
        var comp = document.getElementById('scoreComparison');
        if (comp) comp.textContent = getComparison(report.estimatedCO2Grams);

        // Animate score ring
        var progress = document.getElementById('scoreProgress');
        if (progress) {
            var circumference = 339.292;
            var pct = Math.min(report.estimatedCO2Grams / 3.0, 1.0); // 3g = full ring
            progress.style.strokeDashoffset = String(circumference * (1 - pct));
        }
    }

    function displayAssetBreakdown(assets) {
        var bar = document.getElementById('assetBar');
        var legend = document.getElementById('assetLegend');
        if (!bar || !legend) return;

        bar.innerHTML = '';
        legend.innerHTML = '';

        assets.forEach(function (asset) {
            var catName = typeof asset.category === 'number'
                ? ['HTML','CSS','JavaScript','Images','Fonts','Video','Other'][asset.category] || 'Other'
                : asset.category;

            var seg = document.createElement('div');
            seg.className = 'asset-segment cat-' + catName;
            seg.style.width = Math.max(asset.percentage, 1) + '%';
            seg.title = catName + ': ' + formatBytes(asset.transferSizeBytes);
            if (asset.percentage > 5) seg.textContent = Math.round(asset.percentage) + '%';
            bar.appendChild(seg);

            var item = document.createElement('div');
            item.className = 'legend-item';
            item.innerHTML =
                '<div class="legend-color cat-' + catName + '"></div>' +
                '<div class="legend-info">' +
                '<div class="legend-name">' + escapeHtml(catName) + '</div>' +
                '<div class="legend-size">' + formatBytes(asset.transferSizeBytes) +
                ' (' + asset.resourceCount + ')</div></div>';
            legend.appendChild(item);
        });
    }

    function displaySuggestions(suggestions) {
        var list = document.getElementById('suggestionsList');
        if (!list) return;
        list.innerHTML = '';

        if (!suggestions.length) {
            list.innerHTML = '<div style="text-align:center;color:#10b981;padding:20px;">\u2713 No optimization suggestions \u2014 great job!</div>';
            return;
        }

        // Sort by potential savings descending
        suggestions.sort(function (a, b) { return (b.potentialCO2SavingsGrams || 0) - (a.potentialCO2SavingsGrams || 0); });

        suggestions.forEach(function (s) {
            var sevStr = typeof s.severity === 'number'
                ? ['Low','Medium','High','Critical'][s.severity] || 'Low'
                : s.severity;
            var sevEmoji = { Critical: '\uD83D\uDD34', High: '\uD83D\uDFE0', Medium: '\uD83D\uDFE1', Low: '\uD83D\uDFE2' }[sevStr] || '';

            var details = document.createElement('details');
            details.className = 'suggestion';

            var summary = document.createElement('summary');
            summary.innerHTML =
                '<span class="severity-badge severity-' + sevStr + '">' + sevEmoji + ' ' + escapeHtml(sevStr) + '</span>' +
                '<span class="suggestion-title">' + escapeHtml(s.title) + '</span>' +
                (s.potentialCO2SavingsGrams > 0
                    ? '<span class="suggestion-savings">-' + s.potentialCO2SavingsGrams.toFixed(2) + 'g</span>'
                    : '');

            var body = document.createElement('div');
            body.className = 'suggestion-body';
            body.innerHTML = escapeHtml(s.description || '') +
                (s.affectedAssetUrl
                    ? '<span class="affected-url">' + escapeHtml(s.affectedAssetUrl) + '</span>'
                    : '');

            details.appendChild(summary);
            details.appendChild(body);
            list.appendChild(details);
        });
    }

    // --- Sparkline ---
    function displaySparkline(history) {
        var container = document.getElementById('sparklineContainer');
        if (!container) return;

        if (!history || history.length < 2) {
            container.innerHTML = '<div class="no-history">Not enough data for trend analysis yet.</div>';
            return;
        }

        // Extract CO₂ values ordered oldest → newest
        var values = history.slice().reverse().map(function (r) { return r.estimatedCO2Grams || 0; });
        var svgWidth = 160;
        var svgHeight = 40;
        var max = Math.max.apply(null, values) || 1;
        var min = Math.min.apply(null, values);
        var range = (max - min) || 1;

        var points = values.map(function (v, i) {
            var x = (i / (values.length - 1)) * svgWidth;
            var y = svgHeight - ((v - min) / range) * (svgHeight - 4) - 2;
            return x.toFixed(1) + ',' + y.toFixed(1);
        });

        var newest = values[values.length - 1];
        var oldest = values[0];
        var changePct = oldest > 0 ? ((newest - oldest) / oldest * 100).toFixed(1) : 0;
        var direction = newest < oldest ? 'improving' : newest > oldest ? 'worsening' : 'stable';
        var arrow = newest < oldest ? '\u2193' : newest > oldest ? '\u2191' : '\u2192';

        var scoreColor = { improving: '#22c55e', worsening: '#ef4444', stable: '#6b7280' }[direction];

        container.innerHTML =
            '<svg width="' + svgWidth + '" height="' + svgHeight + '" viewBox="0 0 ' + svgWidth + ' ' + svgHeight + '">' +
            '<polyline fill="none" stroke="' + scoreColor + '" stroke-width="2" points="' + points.join(' ') + '"/>' +
            '</svg>' +
            '<div class="trend-info">' +
            '<div class="trend-direction ' + direction + '">' + arrow + ' ' + Math.abs(changePct) + '%</div>' +
            '<div class="trend-detail">' + (direction === 'improving' ? 'Improving' : direction === 'worsening' ? 'Worsening' : 'Stable') + ' over ' + history.length + ' reports</div>' +
            '</div>';
    }

    // --- Export ---
    function exportReport() {
        if (!currentReport) return;

        var csv = 'Carbon Footprint Report\n\n';
        csv += 'Score,' + currentReport.score + '\n';
        csv += 'CO2 (grams),' + (currentReport.estimatedCO2Grams || 0).toFixed(2) + '\n';
        csv += 'Total Size (bytes),' + (currentReport.totalTransferSizeBytes || 0) + '\n';
        csv += 'Analyzed At,' + (currentReport.analyzedAt || '') + '\n\n';

        csv += 'Asset Breakdown\n';
        csv += 'Category,Size (bytes),Percentage,CO2 (grams),Count\n';
        (currentReport.assets || []).forEach(function (a) {
            var catName = typeof a.category === 'number'
                ? ['HTML','CSS','JavaScript','Images','Fonts','Video','Other'][a.category] || 'Other'
                : a.category;
            csv += catName + ',' + a.transferSizeBytes + ',' + a.percentage.toFixed(2) + '%,' + a.estimatedCO2Grams.toFixed(2) + ',' + a.resourceCount + '\n';
        });

        csv += '\nSuggestions\n';
        csv += 'Severity,Title,Potential Savings (g CO2)\n';
        (currentReport.suggestions || []).forEach(function (s) {
            csv += s.severity + ',' + (s.title || '').replace(/,/g, ';') + ',' + (s.potentialCO2SavingsGrams || 0).toFixed(2) + '\n';
        });

        var blob = new Blob([csv], { type: 'text/csv' });
        var url = URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = url;
        a.download = 'carbon-report-' + new Date().toISOString().split('T')[0] + '.csv';
        a.click();
        URL.revokeObjectURL(url);
    }

    // --- Utilities ---
    function getComparison(co2) {
        if (co2 <= 0.20) return 'Excellent! Cleaner than 95% of websites.';
        if (co2 <= 0.50) return 'Good! Better than average web page.';
        if (co2 <= 1.00) return 'Average carbon footprint.';
        if (co2 <= 2.00) return 'Higher than average. Consider optimizations.';
        return 'Very high carbon footprint. Urgent optimizations needed.';
    }

    function formatBytes(bytes) {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    }

    function escapeHtml(str) {
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(str || ''));
        return div.innerHTML;
    }
})();
