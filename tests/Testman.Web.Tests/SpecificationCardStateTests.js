const test = require("node:test");
const assert = require("node:assert/strict");
const { initializeDisclosureState } = require("../../src/Testman.Web/wwwroot/js/site.js");

function disclosure(key, open = true) {
    const listeners = new Map();
    return {
        dataset: { disclosureKey: key },
        open,
        addEventListener(name, listener) {
            listeners.set(name, listener);
        },
        toggle() {
            listeners.get("toggle")();
        },
    };
}

function root(cards) {
    return {
        querySelectorAll(selector) {
            assert.equal(selector, "details[data-disclosure-key]");
            return cards;
        },
    };
}

test("restores and saves the test list and each specification independently across page loads", () => {
    const values = new Map();
    const storage = {
        getItem: (key) => values.get(key) ?? null,
        setItem: (key, value) => values.set(key, value),
    };
    const firstLoad = [
        disclosure("test-list", false),
        disclosure("specification:C:/specs/one.md"),
        disclosure("specification:C:/specs/two.md", false),
    ];

    values.set("testman:disclosure-open:test-list", "true");
    values.set("testman:disclosure-open:specification:C:/specs/one.md", "false");
    values.set("testman:disclosure-open:specification:C:/specs/two.md", "true");
    initializeDisclosureState(root(firstLoad), storage);

    assert.equal(firstLoad[0].open, true);
    assert.equal(firstLoad[1].open, false);
    assert.equal(firstLoad[2].open, true);
    firstLoad[1].open = true;
    firstLoad[1].toggle();

    const nextLoad = [
        disclosure("test-list", false),
        disclosure("specification:C:/specs/one.md", false),
        disclosure("specification:C:/specs/two.md", false),
    ];
    initializeDisclosureState(root(nextLoad), storage);

    assert.equal(nextLoad[0].open, true);
    assert.equal(nextLoad[1].open, true);
    assert.equal(nextLoad[2].open, true);
});

test("keeps cards usable when session storage is unavailable", () => {
    const specification = disclosure("specification:C:/specs/one.md", false);
    const unavailableStorage = {
        getItem() {
            throw new Error("unavailable");
        },
        setItem() {
            throw new Error("unavailable");
        },
    };

    assert.doesNotThrow(() => initializeDisclosureState(root([specification]), unavailableStorage));
    assert.equal(specification.open, false);
    specification.open = true;
    assert.doesNotThrow(() => specification.toggle());
    assert.equal(specification.open, true);
});
