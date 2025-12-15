import React, { useEffect, useState } from 'react';
import api from '../services/api';
import { useAuth } from '../hooks/useAuth';
import { WeatherWidget } from '../components/WeatherWidget';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { useToast } from '../hooks/useToast';
import { useNavigate } from 'react-router-dom';

interface UserProfile {
  id: string;
  email: string;
  role: string;
  createdAt: string;
}

export const DashboardPage: React.FC = () => {
  const { user, logout } = useAuth();
  const toast = useToast();
  const navigate = useNavigate();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const response = await api.get('/user/profile');
        setProfile(response.data);
      } catch (err) {
        setError('Failed to load profile');
        console.error(err);
      } finally {
        setLoading(false);
      }
    };

    fetchProfile();
  }, []);

  if (loading) {
    return (
      <div className="text-sm text-slate-700">Loading profile…</div>
    );
  }

  if (error) {
    return (
      <div className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800" role="alert">
        {error}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 sm:text-3xl">Dashboard</h1>
          <p className="mt-1 text-sm text-slate-700">Welcome{user?.email ? `, ${user.email}` : ''}.</p>
        </div>

        <Button variant="outline" onClick={logout}>
          Logout
        </Button>
      </header>

      <section className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3" aria-label="Dashboard cards">
        <Card>
          <CardHeader>
            <CardTitle>Account</CardTitle>
          </CardHeader>
          <CardContent>
            {profile ? (
              <dl className="space-y-2 text-sm">
                <div className="flex items-center justify-between gap-3">
                  <dt className="text-slate-700">Role</dt>
                  <dd className="font-medium text-slate-900">{profile.role}</dd>
                </div>
                <div className="flex items-center justify-between gap-3">
                  <dt className="text-slate-700">Member since</dt>
                  <dd className="font-medium text-slate-900">{new Date(profile.createdAt).toLocaleDateString()}</dd>
                </div>
              </dl>
            ) : (
              <div className="text-sm text-slate-700">No profile details available.</div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Quick actions</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              <Button variant="secondary" onClick={() => navigate('/shop')}>
                Browse products
              </Button>
              <Button variant="ghost" onClick={() => navigate('/checkout')}>
                Go to checkout
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Weather</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="max-w-sm">
              <WeatherWidget
                location="Santo Domingo"
                onError={() => toast.error({ title: 'Unable to load weather data' })}
              />
            </div>
          </CardContent>
        </Card>
      </section>
    </div>
  );
};
