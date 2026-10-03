const test = require("node:test");
const assert = require("node:assert/strict");
const { initializeSpecificationCardState } = require("../../src/Testman.Web/wwwroot/js/site.js");

function card(path, open = true) {
    const listeners = new Map();
    return {
        dataset: { specificationPath: path },
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
            assert.equal(selector, "details[data-specification-path]");
            return cards;
        },
    };
}

test("restores and saves each specification card independently across page loads", () => {
    const values = new Map([
        ["testman:specification-open:C:/specs/one.md", "false"],
        ["testman:specification-open:C:/specs/two.md", "true"],
    ]);
    const storage = {
        getItem: (key) => values.get(key) ?? null,
        setItem: (key, value) => values.set(key, value),
    };
    const firstLoad = [card("C:/specs/one.md"), card("C:/specs/two.md", false)];

    initializeSpecificationCardState(root(firstLoad), storage);

    assert.equal(firstLoad[0].open, false);
    assert.equal(firstLoad[1].open, true);
    firstLoad[0].open = true;
    firstLoad[0].toggle();

    const nextLoad = [card("C:/specs/one.md", false), card("C:/specs/two.md", false)];
    initializeSpecificationCardState(root(nextLoad), storage);

    assert.equal(nextLoad[0].open, true);
    assert.equal(nextLoad[1].open, true);
});

test("keeps cards usable when session storage is unavailable", () => {
    const specification = card("C:/specs/one.md", false);
    const unavailableStorage = {
        getItem() {
            throw new Error("unavailable");
        },
        setItem() {
            throw new Error("unavailable");
        },
    };

    assert.doesNotThrow(() => initializeSpecificationCardState(root([specification]), unavailableStorage));
    assert.equal(specification.open, false);
    specification.open = true;
    assert.doesNotThrow(() => specification.toggle());
    assert.equal(specification.open, true);
});
