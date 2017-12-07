<template>
  <v-layout @mouseover='mouseOver' @mouseleave='mouseOut' align-center>
    <v-flex xs2>
      <v-btn icon small xs flat @click.native='visible=!visible' color='grey'>
      <v-icon dark>{{ visible ? "visibility" : "visibility_off" }}</v-icon>
      </v-btn>
    </v-flex>
    <v-flex xs10>
      <div class='subheading'>{{ layer.name }}</div>
      <div class="caption grey--text"> Object count: {{layer.objectCount }} </div>
    </v-flex>
    <v-flex>
      <v-icon dark :style='{ color: layerColor.hex }'>fiber_manual_record</v-icon>
    </v-flex>
  </v-layout>
</template>

<script>

  export default {
    name: '',
    props: {
      layer: Object,
      clientId: String
    },
    components: {},
    computed: {
      layerColor() {
        if( this.layer.properties && this.layer.properties.color )
          return this.layer.properties.color
        return { hex: '#00FF00', alpha: 1 }
      }
    },
    watch: {
      visible( value ) {
        console.log( this.clientId, this.layer.guid, value )
        Interop.setLayerVisibility( this.clientId, this.layer.guid, value )
      }
    },
    data() {
      return {
        visible: true
      }
    },
    methods: {
      mouseOver() {
        Interop.setLayerHover( this.clientId, this.layer.guid, true )
      },
      mouseOut() {
        Interop.setLayerHover( this.clientId, this.layer.guid, false )
      }
    },
    mounted() {
    }
  }
</script>

<style lang='scss'>
    
</style>