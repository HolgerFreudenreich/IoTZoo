#include <unity.h>
#include <stdexcept>
#include <iostream>


int add(int a, int b)
{
    return a + b;
}

int subtract(int a, int b)
{
    return a - b;
}

int multiply(int a, int b)
{
    return a * b;
}

int divide(int a, int b)
{
    if (b == 0)
    {
        throw std::invalid_argument("Division by zero");
    }
    return a / b;
}

void test_add(void)
{
    TEST_ASSERT_EQUAL(5, add(2, 3));
}

void test_subtract(void)
{
    TEST_ASSERT_EQUAL(1, subtract(3, 2));
}

void test_multiply(void)
{
    TEST_ASSERT_EQUAL(6, multiply(2, 3));
}

void test_divide(void)
{
    TEST_ASSERT_EQUAL(2, divide(6, 3));
    TEST_ASSERT_EQUAL(6, divide(6, 1));
    TEST_ASSERT_EQUAL(6, divide(36, 6));
}

void setup()
{
    // set stuff up here
    UNITY_BEGIN();
    RUN_TEST(test_add);
    RUN_TEST(test_subtract);
    RUN_TEST(test_multiply);
    RUN_TEST(test_divide);
    UNITY_END();
}

void loop()
{
    // clean stuff up here
}
