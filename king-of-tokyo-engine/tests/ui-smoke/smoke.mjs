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

try {
  await host.goto(webUrl);
  await host.getByRole('link', { name: 'Create lobby' }).last().click();
  await host.getByLabel('Lobby name').fill('UI smoke test');
  await host.getByLabel('Your display name').fill('Host QA');
  await host.getByRole('button', { name: 'Create lobby' }).click();
  await host.waitForURL(/\/lobbies\/[0-9a-f-]{36}$/i);

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
  await actor.getByRole('button', { name: 'Finalize dice', exact: true }).click();

  assert.equal(await actor.locator('.die-button').count() >= 6, true);
  assert.equal(await actor.locator('.market-card').count() >= 3, true);
  await actor.screenshot({ path: new URL('game-desktop.png', artifacts).pathname, fullPage: true });
  await actor.setViewportSize({ width: 390, height: 844 });
  await actor.screenshot({ path: new URL('game-mobile.png', artifacts).pathname, fullPage: true });

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
