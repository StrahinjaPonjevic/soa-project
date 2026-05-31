<?php

namespace App\Http\Middleware;

use Closure;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;

class JwtAuthMiddleware
{
    public function handle(Request $request, Closure $next): Response
    {
        $authorization = $request->header('Authorization');
        if (!$authorization || !str_starts_with($authorization, 'Bearer ')) {
            return $this->unauthorized('Missing bearer token.');
        }

        $token = trim(substr($authorization, 7));
        $parts = explode('.', $token);
        if (count($parts) !== 3) {
            return $this->unauthorized('Invalid token format.');
        }

        [$headerB64, $payloadB64, $signatureB64] = $parts;

        $header = $this->decodeJson($headerB64);
        $payload = $this->decodeJson($payloadB64);
        if ($header === null || $payload === null) {
            return $this->unauthorized('Invalid token payload.');
        }

        if (($header['alg'] ?? null) !== 'HS256') {
            return $this->unauthorized('Unsupported token algorithm.');
        }

        $jwtKey = config('app.jwt_key');
        if (!$jwtKey) {
            return response()->json(['message' => 'Server JWT key is not configured.'], 500);
        }

        $expectedSignature = hash_hmac('sha256', $headerB64.'.'.$payloadB64, $jwtKey, true);
        $tokenSignature = $this->base64UrlDecode($signatureB64);
        if ($tokenSignature === false || !hash_equals($expectedSignature, $tokenSignature)) {
            return $this->unauthorized('Invalid token signature.');
        }

        $exp = $payload['exp'] ?? null;
        if (!is_int($exp) && !ctype_digit((string) $exp)) {
            return $this->unauthorized('Token expiration is invalid.');
        }
        if ((int) $exp <= time()) {
            return $this->unauthorized('Token expired.');
        }

        $userId = $payload['userId'] ?? null;
        if (!is_int($userId) && !ctype_digit((string) $userId)) {
            return $this->unauthorized('Missing userId claim.');
        }

        $role = $payload['role']
            ?? $payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
            ?? null;
        if ($role !== 'Tourist') {
            return response()->json(['message' => 'Only Tourist role is allowed.'], 403);
        }

        $request->attributes->set('auth.user_id', (int) $userId);
        $request->attributes->set('auth.role', $role);

        return $next($request);
    }

    private function decodeJson(string $value): ?array
    {
        $decoded = $this->base64UrlDecode($value);
        if ($decoded === false) {
            return null;
        }

        $json = json_decode($decoded, true);
        return is_array($json) ? $json : null;
    }

    private function base64UrlDecode(string $value): string|false
    {
        $remainder = strlen($value) % 4;
        if ($remainder !== 0) {
            $value .= str_repeat('=', 4 - $remainder);
        }

        return base64_decode(strtr($value, '-_', '+/'), true);
    }

    private function unauthorized(string $message): JsonResponse
    {
        return response()->json(['message' => $message], 401);
    }
}
