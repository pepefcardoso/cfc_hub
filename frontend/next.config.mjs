/** @type {import('next').NextConfig} */
const nextConfig = {
  output: process.env.NODE_ENV === "development" ? undefined : "standalone",
  experimental: {
    cpus: 2,
  },
  webpack: (config, { dev }) => {
    if (dev) {
      config.parallelism = 1;
    }
    return config;
  },
};

export default nextConfig;
