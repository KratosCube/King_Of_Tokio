import assert from 'node:assert/strict';
import { mkdir } from 'node:fs/promises';
import { chromium } from 'playwright';

const webUrl = 'http://localhost:5173';
const artifacts = new URL('./artifacts/', import.meta.url);
await mkdir(artifacts, { recursive: true });

const browser = await chromium.launch({ headless: true });
const errors = [];
const hostContext = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const guestContext = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const host = await hostContext.newPage();
const guest = await guestContext.newPage();
for (const page of [host, guest]) {
  page.setDefaultTimeout(20000);
  page.on('pageerror', error => errors.push(error.message));
}

async function pageWithButton(pages, label) {
  for (let attempt = 0; attempt < 40; attempt++) {
    for (const page of pages) {
      if (await page.getByRole('button', { name: label, exact: true }).count() > 0) {
        return page;
      }
    }
    await new Promise(resolve => setTimeout(resolve, 250));
  }
  throw new Error(`No player was offered ${label}`);
}

async function capture(page, name, heading, mobile = false) {
  await page.setViewportSize(mobile ? { width: 390, height: 844 } : { width: 1440, height: 900 });
  await page.evaluate(() => window.scrollTo(0, 0));
  const titleBox = await page.getByRole('heading', { name: heading }).boundingBox();
  assert.ok(titleBox && titleBox.y >= 0, `${heading} must be visible in ${name}`);
  const width = await page.evaluate(() => document.documentElement.scrollWidth);
  assert.ok(width <= (mobile ? 390 : 1440), `${name} must not overflow horizontally (${width}px)`);
  await page.screenshot({ path: new URL(`${name}.png`, artifacts).pathname, fullPage: true });
}

try {
  await host.goto(webUrl);
  await capture(host, 'home-desktop', 'Become the King of Tokyo');
  await capture(host, 'home-mobile', 'Become the King of Tokyo', true);
  await host.setViewportSize({ width: 1440, height: 900 });
  await host.getByRole('link', { name: 'Create lobby' }).last().click();
  await capture(host, 'create-desktop', 'Create lobby');
  await capture(host, 'create-mobile', 'Create lobby', true);
  await host.setViewportSize({ width: 1440, height: 900 });
  await host.getByLabel('Lobby name').fill('UI smoke test');
  await host.getByLabel('Your display name').fill('Host QA');
  await host.getByRole('button', { name: 'Create lobby' }).click();
  await host.waitForURL(/\/lobbies\/[0-9a-f-]{36}$/i);
  await capture(host, 'lobby-desktop', 'Lobby');
  await capture(host, 'lobby-mobile', 'Lobby', true);
  await host.setViewportSize({ width: 1440, height: 900 });

  const invite = await host.getByLabel('Invite link').inputValue();
  assert.match(invite, /\/lobbies\/[0-9a-f-]{36}$/i);
  await guest.goto(invite);
  await guest.getByLabel('Your display name').fill('Guest QA');
  await guest.getByLabel('Your monster').selectOption({ index: 1 });
  await guest.getByRole('button', { name: 'Join', exact: true }).click();
  assert.equal(await guest.getByText('Cyber Kitty', { exact: true }).count(), 1);
  await guest.getByRole('button', { name: 'Set ready', exact: true }).click();

  const hostSession = await host.evaluate(() => JSON.parse(localStorage.getItem('king-of-tokyo.client-session')));
  const guestSession = await guest.evaluate(() => JSON.parse(localStorage.getItem('king-of-tokyo.client-session')));
  assert.notEqual(hostSession.PlayerId, guestSession.PlayerId);
  assert.notEqual(hostSession.PlayerToken, guestSession.PlayerToken);

  await host.getByRole('button', { name: 'Start game' }).click();
  await host.waitForURL(/\/games\/[0-9a-f-]{36}$/i);
  await guest.getByRole('link', { name: 'Open game' }).click();
  await guest.waitForURL(/\/games\/[0-9a-f-]{36}$/i);

  assert.equal(await host.locator('.dev-control-player-button').count(), 0);
  await host.getByRole('button', { name: 'Initialize game' }).click();
  const actor = await pageWithButton([host, guest], 'Begin turn');
  const next = actor === host ? guest : host;
  await actor.getByRole('button', { name: 'Begin turn' }).click();
  await actor.getByRole('button', { name: 'Roll dice', exact: true }).click();
  await actor.locator('.die-button').first().click();
  assert.match(await actor.locator('.die-button').first().getAttribute('class'), /selected/);
  await actor.getByRole('button', { name: 'Clear dice selection' }).click();
  await actor.getByRole('button', { name: 'Finalize dice', exact: true }).click();

  assert.equal(await actor.locator('.die-button').count() >= 6, true);
  assert.equal(await actor.locator('.market-card').count() >= 3, true);
  const energy = Number((await actor.locator('.monster-card.selected .monster-stats span').last().innerText()).match(/\d+/)?.[0]);
  assert.ok(Number.isInteger(energy), 'The local monster must display its energy');
  for (const card of await actor.locator('.market-card').all()) {
    const buy = card.getByRole('button', { name: 'Buy', exact: true });
    if (await buy.count() === 0) continue;
    const cost = Number((await card.locator('.market-cost').innerText()).match(/\d+/)?.[0]);
    assert.equal(await buy.isDisabled(), energy < cost, 'Buy must reflect available energy');
  }
  for (const refresh of await actor.getByRole('button', { name: 'Refresh market (⚡ 2)' }).all()) {
    assert.equal(await refresh.isDisabled(), energy < 2, 'Refresh costs two energy');
  }
  await actor.getByText('Dice resolved', { exact: true }).waitFor();
  await actor.reload();
  await actor.getByText('Dice resolved', { exact: true }).waitFor();
  await capture(actor, 'game-desktop', 'Tokyo arena');
  await capture(actor, 'game-mobile', 'Tokyo arena', true);

  await actor.getByRole('button', { name: 'End turn', exact: true }).click();
  await actor.getByRole('button', { name: 'Advance player', exact: true }).click();
  await next.getByRole('button', { name: 'Begin turn', exact: true }).click();
  await next.getByRole('button', { name: 'Roll dice', exact: true }).waitFor();
  assert.deepEqual(errors, []);
  console.log('Browser smoke test passed: lobby, two seats, first turn, and next player.');
} catch (error) {
  await Promise.allSettled([
    host.screenshot({ path: new URL('failure-host.png', artifacts).pathname, fullPage: true }),
    guest.screenshot({ path: new URL('failure-guest.png', artifacts).pathname, fullPage: true })
  ]);
  throw error;
} finally {
  await browser.close();
}
