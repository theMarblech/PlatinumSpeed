//-----------------------------------------------------------------------------
// Blast.cs
//
// Hax central, but works
//
// Implemented for any Marble Blast; any version
// So long as a few things are kept similar
//
// From Project Revolution
// The MultiPlayer Marble Blast experience
//
// Copyright (c) 2021 The Platinum Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Blast Update Stuff

// I am updating this to be client sided so that it makes it faster
// it is called in onFrameAdvance in playGui.cs
function serverBlastUpdate() {
	cancel($MP::Schedule::BlastUpdate);
	if ($Server::Dedicated && getRealPlayerCount() == 0)
		return;

	//H-h-h-hard coded!
	%timeDelta = 100;

	for (%i = 0; %i < ClientGroup.getCount(); %i ++) {
		%client = ClientGroup.getObject(%i);
		if (!%client.playing)
			continue;

		%blastValue = %client.blastValue;
		// Update blast value
		%blastValue += (%timeDelta / $MP::BlastChargeTime);
		// Normalize blast value
		//Keep it 0 < value < 1
		if (%client.usingSpecialBlast)
			%blastValue = 1;
		if (%blastValue < 0)
			%blastValue = 0;
		else if (%blastValue > 1)
			%blastValue = 1;
		%client.blastValue = %blastValue; // Doesn't send to client
	}

	$MP::Schedule::BlastUpdate = schedule(%timeDelta, 0, serverBlastUpdate);
}

//-----------------------------------------------------------------------------
// Blast function
// Where the knitty gritty is done

function serverCmdBlast(%client, %gravity) {
	// CANCEL THE CANNON.
	if (%client.isInCannon()) {
		%client.cancelCannon(true);
		return;
	}

	//Display blast particles and make explosion
	%client.makeBlastParticle(%gravity);
	ServerPlay3D(blastSfx, %client.player.getWorldBoxCenter());
	%client.setSpecialBlast(false);
	%client.setBlastValue(0); // Sends to client
}

//-----------------------------------------------------------------------------
// Blast Particle
//-----------------------------------------------------------------------------

datablock ParticleData(BlastSmoke) {
	textureName          = "~/data/particles/smoke";
	dragCoefficient      = 1;
	gravityCoefficient   = 0;
	inheritedVelFactor   = 0;
	windCoefficient      = 0;
	constantAcceleration = 0;
	lifetimeMS           = 500;
	lifetimeVarianceMS   = 100;
	spinSpeed     = 20;
	spinRandomMin = 0.0;
	spinRandomMax = 0.0;
	useInvAlpha   = true;

	colors[0]     = "0 1 1 0.1";
	colors[1]     = "0 1 1 0.5";
	colors[2]     = "0 1 1 0.9";

	sizes[0]      = 0.125;
	sizes[1]      = 0.125;
	sizes[2]      = 0.125;

	times[0]      = 0.0;
	times[1]      = 0.4;
	times[2]      = 1.0;
};

datablock ParticleData(UltraBlastSmoke) {
	textureName          = "~/data/particles/smoke";
	dragCoefficient      = 1;
	gravityCoefficient   = 0;
	inheritedVelFactor   = 0;
	windCoefficient      = 0;
	constantAcceleration = 0;
	lifetimeMS           = 500;
	lifetimeVarianceMS   = 100;
	spinSpeed     = 20;
	spinRandomMin = 0.0;
	spinRandomMax = 0.0;
	useInvAlpha   = true;

	colors[0]     = "1 0.7 0 0.1";
	colors[1]     = "1 0.7 0 0.5";
	colors[2]     = "1 0.7 0 0.9";

	sizes[0]      = 0.125;
	sizes[1]      = 0.125;
	sizes[2]      = 0.125;

	times[0]      = 0.0;
	times[1]      = 0.4;
	times[2]      = 1.0;
};

datablock ParticleEmitterData(BlastEmitter) {
	ejectionPeriodMS = 1;
	periodVarianceMS = 0;
	ejectionVelocity = 4;
	velocityVariance = 0;
	ejectionOffset   = 0;
	thetaMin         = 90;
	thetaMax         = 100;
	phiReferenceVel  = 0;
	phiVariance      = 360;
	lifetimeMS       = 500;
	particles        = "BlastSmoke";
};

datablock ParticleEmitterData(UltraBlastEmitter) {
	ejectionPeriodMS = 1;
	periodVarianceMS = 0;
	ejectionVelocity = 4;
	velocityVariance = 0;
	ejectionOffset   = 0;
	thetaMin         = 90;
	thetaMax         = 100;
	phiReferenceVel  = 0;
	phiVariance      = 360;
	lifetimeMS       = 500;
	particles        = "UltraBlastSmoke";
};

function Marble::sendShockwave(%this, %strength) {

	echo("Marble" SPC %this SPC "sending a shockwave of strength" SPC %strength);
	%mePos = %this.getWorldBoxCenter();

	for (%i = 0; %i < ClientGroup.getCount(); %i ++) {
		%client = ClientGroup.getObject(%i);

		if (%client == %this.client) {
			continue;
		}

		//Don't blast people who cannot be blasted
		if (%client.spectating || !isObject(%client.player)) {
			continue;
		}

		// If they are frozen on the ice shard, they shouldn't be able to receive
		// the blast impulse from other players.
		if (%client.player.isFrozen)
			continue;

		%theyPos = %client.player.getWorldBoxCenter();
		%myMod = %this.getDataBlock().blastModifier;
		%theyMod = %client.player.getDataBlock().blastModifier;
		commandToClient(%client, 'Shockwave', %mePos, %strength, %myMod, %theyMod);

//		echo("Dist is" SPC %this.getBlastRadius(%strength, %client.player) SPC "to" SPC %client.getUsername());

		Mode::callback("onBlast", "", new ScriptObject() {
			this = %this;
			other = %client.player;
			strength = %strength;
			_delete = true;
		});
//      echo("Blast:" SPC %client SPC "mepos:" SPC %mePos SPC "strength:" SPC %strength SPC "mymod:" SPC %myMod SPC "theymod:" SPC %theyMod);
	}
}

// TODO: UTALIZE THE GRAVITY (its getGravityDir() but MP friendly)
function GameConnection::makeBlastParticle(%this, %gravity) {
	%this.player.sendShockwave(%this.blastValue * (%this.usingSpecialBlast ? $MP::BlastRechargeShockwaveStrength : $MP::BlastShockwaveStrength));

	// get the blast particles
	%emitter = (%this.usingSpecialBlast ? UltraBlastEmitter : BlastEmitter);
	%this.transferParticles(%emitter, false, %gravity);
}