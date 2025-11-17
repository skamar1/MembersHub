#!/usr/bin/env python3
"""
Test script to check the "Νέο Χρήστη" error on membershub.gr
"""
import asyncio
from playwright.async_api import async_playwright
import sys

async def test_new_user_error():
    async with async_playwright() as p:
        # Launch browser in headless mode
        browser = await p.chromium.launch(headless=True)
        context = await browser.new_context(
            viewport={'width': 1920, 'height': 1080},
            locale='el-GR'
        )
        page = await context.new_page()

        # Enable console logging
        page.on("console", lambda msg: print(f"CONSOLE [{msg.type}]: {msg.text}"))

        # Enable error logging
        page.on("pageerror", lambda err: print(f"PAGE ERROR: {err}"))

        try:
            print("1. Navigating to https://membershub.gr ...")
            await page.goto("https://membershub.gr", wait_until="networkidle", timeout=30000)
            print(f"   ✓ Page loaded: {page.url}")

            # Take screenshot of login page
            await page.screenshot(path="/tmp/membershub_login.png")
            print("   ✓ Screenshot saved: /tmp/membershub_login.png")

            # Find login form
            print("\n2. Looking for login form...")
            username_input = page.locator('input[type="text"]').first
            password_input = page.locator('input[type="password"]').first

            if await username_input.count() == 0:
                print("   ✗ Username input not found!")
                return

            print("   ✓ Login form found")

            # Try to login with admin credentials
            print("\n3. Attempting to login...")

            # Clear and fill username
            await username_input.click()
            await username_input.fill("")
            await username_input.type("admin", delay=50)
            # Trigger blur event to validate
            await username_input.press("Tab")
            print("   - Username entered")

            await page.wait_for_timeout(300)

            # Password should now be focused
            await password_input.fill("")
            await password_input.type("Aris100*", delay=50)
            print("   - Password entered")

            # Trigger blur to validate password field
            await page.keyboard.press("Tab")
            await page.wait_for_timeout(1500)

            # Look for Sign In button that is enabled
            sign_in_button = page.locator('button:has-text("Sign In"), button:has-text("Σύνδεση")').first

            # Wait for button to be enabled
            try:
                await sign_in_button.wait_for(state="visible", timeout=3000)
                is_disabled = await sign_in_button.is_disabled()
                print(f"   - Button found, disabled: {is_disabled}")

                if is_disabled:
                    # Try pressing Enter instead
                    print("   - Button is disabled, trying Enter key...")
                    await password_input.press("Enter")
                else:
                    await sign_in_button.click()

            except Exception as e:
                print(f"   - Could not find button, trying Enter: {e}")
                await password_input.press("Enter")

            # Wait for actual page change (not just URL change)
            print("   - Waiting for dashboard to load...")

            try:
                # Wait for navigation away from login page
                await page.wait_for_function("window.location.pathname !== '/login'", timeout=8000)
                await page.wait_for_load_state("networkidle", timeout=5000)
                print(f"   ✓ Logged in successfully! URL: {page.url}")
            except Exception as e:
                # Check for error message
                error_msg = await page.locator('.mud-alert-message, .mud-snackbar, [role="alert"]').first.text_content() if await page.locator('.mud-alert-message, .mud-snackbar, [role="alert"]').count() > 0 else None
                if error_msg:
                    print(f"   ✗ Login failed with error: {error_msg}")
                else:
                    print(f"   ✗ Login timeout or failed. Current URL: {page.url}")
                    print(f"      Error: {str(e)[:100]}")
                    # Don't continue if login failed
                    return

            # Take screenshot after login
            await page.screenshot(path="/tmp/membershub_dashboard.png")
            print("   ✓ Screenshot saved: /tmp/membershub_dashboard.png")

            # Navigate to Users page
            print("\n4. Navigating to Users page...")

            # Wait for the page to fully load
            await page.wait_for_timeout(2000)

            # Try to open drawer/hamburger menu if exists
            hamburger = page.locator('button[aria-label="Open drawer"], button:has(svg):has-text("Menu"), .mud-icon-button').first
            if await hamburger.count() > 0:
                try:
                    await hamburger.click()
                    await page.wait_for_timeout(1000)
                    print("   - Opened navigation menu")
                except:
                    pass

            # Try to find sidebar menu or navigation
            # Look for "Χρήστες" or "Users" link
            selectors_to_try = [
                'a:has-text("Χρήστες")',
                'a:has-text("Users")',
                '[href="/users"]',
                'nav a:has-text("Χρήστες")',
            ]

            users_link = None
            for selector in selectors_to_try:
                link = page.locator(selector).first
                if await link.count() > 0:
                    # Check if visible
                    is_visible = await link.is_visible()
                    users_link = link
                    print(f"   - Found users link with selector: {selector}, visible: {is_visible}")
                    if is_visible:
                        break

            if users_link:
                try:
                    await users_link.click()
                    await page.wait_for_load_state("networkidle", timeout=5000)
                    print(f"   ✓ Navigated to Users page: {page.url}")
                except Exception as e:
                    print(f"   ✗ Error clicking users link: {str(e)[:100]}")
                    # Try direct URL as fallback
                    await page.goto("https://membershub.gr/users")
                    await page.wait_for_load_state("networkidle")
                    print(f"   - Navigated directly to: {page.url}")
            else:
                print("   - Users link not found in menu, trying direct URL...")
                await page.goto("https://membershub.gr/users")
                await page.wait_for_load_state("networkidle")
                print(f"   - Navigated directly to: {page.url}")

            await page.screenshot(path="/tmp/membershub_users.png")
            print("   ✓ Screenshot saved: /tmp/membershub_users.png")

            # Look for "Νέο" or "Νέος Χρήστης" button
            print("\n5. Looking for 'Νέο Χρήστη' button...")

            # Try multiple selectors
            new_user_button = None

            # Try text matching
            button_selectors = [
                page.get_by_text("Νέο", exact=False),
                page.get_by_text("Νέος", exact=False),
                page.locator('button:has-text("Νέο")'),
                page.locator('a:has-text("Νέο")'),
            ]

            for selector in button_selectors:
                if await selector.count() > 0:
                    new_user_button = selector.first
                    print(f"   ✓ Found button: {await new_user_button.text_content()}")
                    break

            if not new_user_button:
                print("   ✗ 'Νέο Χρήστη' button not found!")
                print("   Available buttons:")
                buttons = await page.locator('button').all()
                for btn in buttons[:10]:  # Show first 10
                    text = await btn.text_content()
                    print(f"     - {text}")
                return

            # Click the button and wait for any errors
            print("\n6. Clicking 'Νέο Χρήστη' button...")

            # Set up error handler
            errors_found = []

            def handle_console_error(msg):
                if msg.type == "error":
                    errors_found.append(msg.text)
                    print(f"   ✗ CONSOLE ERROR: {msg.text}")

            page.on("console", handle_console_error)

            await new_user_button.click()

            # Wait a bit for dialog to appear or error to occur
            await page.wait_for_timeout(2000)

            # Take screenshot after clicking
            await page.screenshot(path="/tmp/membershub_new_user_clicked.png")
            print("   ✓ Screenshot saved: /tmp/membershub_new_user_clicked.png")

            # Check if dialog appeared
            dialog = page.locator('[role="dialog"]').first
            if await dialog.count() > 0:
                print("   ✓ Dialog appeared successfully")
                dialog_text = await dialog.text_content()
                print(f"   Dialog content preview: {dialog_text[:200]}...")
            else:
                print("   ✗ No dialog appeared!")

            # Summary
            print("\n" + "="*60)
            print("SUMMARY:")
            print("="*60)
            if errors_found:
                print(f"✗ Found {len(errors_found)} error(s):")
                for i, error in enumerate(errors_found, 1):
                    print(f"\n  Error {i}:")
                    print(f"  {error}")
            else:
                print("✓ No JavaScript errors detected")

            print("\nScreenshots saved to /tmp/:")
            print("  - membershub_login.png")
            print("  - membershub_dashboard.png")
            print("  - membershub_users.png")
            print("  - membershub_new_user_clicked.png")

        except Exception as e:
            print(f"\n✗ ERROR: {e}")
            import traceback
            traceback.print_exc()

            # Take error screenshot
            try:
                await page.screenshot(path="/tmp/membershub_error.png")
                print("Error screenshot saved: /tmp/membershub_error.png")
            except:
                pass

        finally:
            await browser.close()

if __name__ == "__main__":
    asyncio.run(test_new_user_error())
